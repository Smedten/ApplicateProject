using Applicate.Domain.Data;
using Applicate.Domain.Entities;
using Applicate.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Applicate.Domain;

public class ActionService
{
    private readonly ResourceService _resourceService;
    private readonly AppDbContext _dbContext;
    private readonly IHttpContextAccessor _contextAccessor;

    public ActionService(ResourceService resourceService, AppDbContext dbContext, IHttpContextAccessor contextAccessor)
    {
        _resourceService = resourceService;
        _dbContext = dbContext;
        _contextAccessor = contextAccessor;
    }

    public async Task ExecuteActionAsync(string resourceName, string actionName, Dictionary<string, string> parameters)
    {
        var resourceDef = _resourceService.GetResource(resourceName);
        if (resourceDef == null) throw new Exception($"Resource '{resourceName}' not found.");

        var actionDef = resourceDef.Actions.FirstOrDefault(a => a.Name.Equals(actionName, StringComparison.OrdinalIgnoreCase));
        if (actionDef == null) throw new Exception($"Action '{actionName}' not found.");

        if (actionDef.Roles != null && actionDef.Roles.Any())
        {
            var user = _contextAccessor.HttpContext?.User;

            if (user == null || !user.Identity.IsAuthenticated)
                throw new UnauthorizedAccessException("Du skal være logget ind for at udføre denne handling.");

            bool hasAccess = false;
            foreach (var role in actionDef.Roles)
            {
                if (user.IsInRole(role))
                {
                    hasAccess = true;
                    break;
                }
            }

            if (!hasAccess)
                throw new UnauthorizedAccessException($"Adgang nægtet. Kræver en af følgende roller: {string.Join(", ", actionDef.Roles)}");
        }

        // 1. Find ID og Entity
        if (!parameters.TryGetValue("id", out var idStr))
            throw new Exception("Action kræver et 'id'.");

        var entityType = TypeRegistry.GetType(resourceName);
        if (entityType == null) throw new Exception($"Unknown type for {resourceName}");

        // Reflection: Kald _dbContext.FindAsync(entityType, guid)
        if (!Guid.TryParse(idStr, out var guid)) throw new Exception("Invalid GUID");

        var entity = await _dbContext.FindAsync(entityType, guid);
        if (entity == null) throw new Exception($"{resourceName} with id {guid} not found.");

        // 2. KØR WORKFLOW (Generic Engine) 🚀
        if (actionDef.Steps != null)
        {
            foreach (var step in actionDef.Steps)
            {
                await ExecuteStep(step, entity, entityType);
            }
        }
        else
        {
            // Fallback til hardcoded (hvis man stadig har gammel logik)
            // Men i princippet burde vi slette dette nu!
            Console.WriteLine($"[WARNING] Action {actionName} has no steps defined via JSON.");
        }

        // Audit logging
        var Loguser = _contextAccessor.HttpContext?.User;
        var username = Loguser?.Identity?.Name ?? "System"; // Hent navnet fra Token

        var log = new LogEntity
        {
            Id = Guid.NewGuid(),
            ResourceName = resourceName,
            ActionName = actionName,
            Username = username,
            Details = $"Executed action on {idStr}. Params: {string.Join(", ", parameters.Select(kv => kv.Key + "=" + kv.Value))}",
            Timestamp = DateTime.UtcNow,
        };

        _dbContext.Add(log);

        await _dbContext.SaveChangesAsync();
    }

    private async Task ExecuteStep(WorkflowStep step, object entity, Type entityType)
    {
        switch (step.Type)
        {
            case "UpdateField":
                var fieldName = step.Args["field"];
                var valueStr = step.Args["value"];

                // Reflection: Find property på C# klassen
                var prop = entityType.GetProperty(fieldName);
                if (prop == null) throw new Exception($"Field '{fieldName}' not found on {entityType.Name}");

                // Konverter string værdi til property type (f.eks. "Confirmed" -> string, "100" -> decimal)
                // Her laver vi en simpel string konvertering, men kan udvides
                prop.SetValue(entity, valueStr);
                break;

            case "Validate":
                // Avanceret: Tjek om en værdi overholder en regel
                var checkField = step.Args["field"];
                var checkValue = step.Args["value"];
                var op = step.Args["op"];
                var errorMsg = step.Args.ContainsKey("error") ? step.Args["error"] : "Validation failed";

                var checkProp = entityType.GetProperty(checkField);
                var actualValue = checkProp?.GetValue(entity)?.ToString();

                if (op == "==" && actualValue != checkValue) throw new Exception(errorMsg);
                if (op == "!=" && actualValue == checkValue) throw new Exception(errorMsg);
                break;

            default:
                throw new Exception($"Unknown workflow step: {step.Type}");
        }
    }
}