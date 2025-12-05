using Applicate.Domain;
using Applicate.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace Applicate.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ResourcesController : ControllerBase
{
    private readonly ResourceService _resourceService;
    private readonly QueryExecutor _queryExecutor;
    private readonly ActionService _actionService;

    public ResourcesController(
        ResourceService resourceService,
        QueryExecutor queryExecutor,
        ActionService actionService)
    {
        _resourceService = resourceService;
        _queryExecutor = queryExecutor;
        _actionService = actionService;
    }

    // GET api/resources
    [HttpGet]
    public IActionResult ListResources()
    {
        var list = _resourceService.GetAllResources()
            .Select(r => new { r.Name, Kind = r.Kind.ToString() });
        return Ok(list);
    }

    // GET api/resources/Booking
    [HttpGet("{name}")]
    public IActionResult GetSpec(string name)
    {
        var resource = _resourceService.GetResource(name);
        if (resource == null) return NotFound("Resource not found");
        return Ok(resource);
    }

    // POST api/resources/{queryName}/run
    // Dette endpoint kører dine Queries (f.eks. ActiveBookings)
    [HttpPost("{resourceName}/run")]
    public async Task<IActionResult> RunQuery(string resourceName)
    {
        var resource = _resourceService.GetResource(resourceName);
        if (resource == null) return NotFound("Resource not found");

        try
        {
            if (resource.Kind == ResourceKind.Query)
            {
                var result = await _queryExecutor.ExecuteQueryAsync(resourceName);
                return Ok(result);
            }
            else
            {
                return BadRequest($"Resource '{resourceName}' is not a Query.");
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPost("{resourceName}/data")]
    public async Task<IActionResult> CreateData(string resourceName, [FromBody] System.Text.Json.JsonElement body, [FromServices] DataService dataService)
    {
        try
        {
            var createdEntity = await dataService.CreateAsync(resourceName, body);
            // Returner 201 Created og det nye objekt
            return Created($"/api/resources/{resourceName}", createdEntity);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // POST api/resources/{resourceName}/action/{actionName}
    // Eksempel: POST api/resources/Booking/action/SignContract?id=...
    [HttpPost("{resourceName}/action/{actionName}")]
    public async Task<IActionResult> RunAction(string resourceName, string actionName)
    {
        try
        {
            // Vi tager parametre fra Query String (f.eks. ?id=123&reason=test)
            // Konverter IQueryCollection til Dictionary<string, string>
            var parameters = Request.Query.ToDictionary(
                q => q.Key,
                q => q.Value.ToString()
            );

            await _actionService.ExecuteActionAsync(resourceName, actionName, parameters);
            return Ok(new { message = $"Action '{actionName}' executed successfully." });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}