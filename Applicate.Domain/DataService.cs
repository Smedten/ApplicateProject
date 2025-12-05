using Applicate.Domain.Data;
using Applicate.Domain.Entities;
using Applicate.Domain.Models;
using System.Text.Json;

namespace Applicate.Domain;

public class DataService
{
    private readonly AppDbContext _dbContext;
    private readonly ResourceService _resourceService;

    public DataService(AppDbContext dbContext, ResourceService resourceService)
    {
        _dbContext = dbContext;
        _resourceService = resourceService;
    }

    public async Task<object> CreateAsync(string resourceName, JsonElement jsonBody)
    {
        // 1. Valider at ressourcen findes og er en Tabel
        var resourceDef = _resourceService.GetResource(resourceName);
        if (resourceDef == null || resourceDef.Kind != ResourceKind.Table)
            throw new Exception($"'{resourceName}' er ikke en tabel-ressource.");

        // 2. Find C# typen (f.eks. BookingEntity)
        var entityType = TypeRegistry.GetType(resourceName);
        if (entityType == null)
            throw new Exception($"Systemet kender ikke C# typen for '{resourceName}'. Husk at opdatere TypeRegistry.");

        // 3. Deserialiser JSON til den rigtige klasse 
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var entityInstance = JsonSerializer.Deserialize(jsonBody, entityType, options);

        if (entityInstance == null)
            throw new Exception("Kunne ikke læse dataen.");

        ValidateData(entityInstance, resourceDef, jsonBody);

        // 4. Gem i databasen
        // DbContext.Add() er smart nok til at tage imod 'object', så længe typen er kendt af EF Core
        _dbContext.Add(entityInstance);
        await _dbContext.SaveChangesAsync();

        return entityInstance;
    }

    private void ValidateData(object entity, ResourceDef resourceDef, JsonElement jsonBody)
    {
        foreach (var field in resourceDef.Fields)
        {
            if (field.Validation == null) continue;

            // Brug Reflection til at hente den faktiske værdi fra objektet
            var prop = entity.GetType().GetProperty(field.Name);
            var value = prop?.GetValue(entity);

            // Regel: Required
            if (field.Validation.Required)
            {
                // Tjek for null, tom string eller tomt Guid
                if (value == null ||
                   (value is string s && string.IsNullOrWhiteSpace(s)) ||
                   (value is Guid g && g == Guid.Empty))
                {
                    throw new Exception($"Feltet '{field.Name}' er påkrævet.");
                }
            }

            // Regel: Min / Max (Kun for tal)
            if (value != null && (field.DataType == "decimal" || field.DataType == "int"))
            {
                var numVal = Convert.ToDouble(value);
                if (field.Validation.Min.HasValue && numVal < field.Validation.Min.Value)
                    throw new Exception($"'{field.Name}' skal være mindst {field.Validation.Min.Value}.");

                if (field.Validation.Max.HasValue && numVal > field.Validation.Max.Value)
                    throw new Exception($"'{field.Name}' må højest være {field.Validation.Max.Value}.");
            }
        }
    }
}