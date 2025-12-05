using Applicate.Domain.Data;
using Applicate.Domain.Entities;
using Applicate.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Applicate.Domain;

public class QueryExecutor
{
    private readonly AppDbContext _dbContext;
    private readonly ResourceService _resourceService;

    public QueryExecutor(AppDbContext dbContext, ResourceService resourceService)
    {
        _dbContext = dbContext;
        _resourceService = resourceService;
    }

    public async Task<List<Dictionary<string, object>>> ExecuteQueryAsync(string queryName)
    {
        // 1. Find opskriften (JSON)
        var resource = _resourceService.GetResource(queryName);

        if (resource == null)
            throw new Exception($"Ressourcen '{queryName}' findes ikke.");

        if (resource.Kind != ResourceKind.Query || resource.Query == null)
            throw new Exception($"'{queryName}' er ikke en Query resource.");

        var def = resource.Query;

        // 1. Vælg Tabel (Source)
        if (def.Source.Equals("Booking", StringComparison.OrdinalIgnoreCase))
        {
            return await ExecuteBookingQuery(def);
        }
        else if (def.Source.Equals("Customer", StringComparison.OrdinalIgnoreCase))
        {
            return await ExecuteCustomerQuery(def);
        }
        else if (def.Source.Equals("House", StringComparison.OrdinalIgnoreCase))
        {
            return await ExecuteHouseQuery(def);
        }
        else if (def.Source.Equals("SystemLog", StringComparison.OrdinalIgnoreCase))
        {
            return await ExecuteLogQuery(def);
        }
        else
        {
            throw new Exception($"QueryExecutor kender ikke kilden '{def.Source}' endnu.");
        }
    }

    // Specifik handler for Bookings (Din gamle logik flyttet herned)
    private async Task<List<Dictionary<string, object>>> ExecuteBookingQuery(QueryDefinition def)
    {
        var query = _dbContext.Bookings.AsQueryable();

        // INCLUDES
        if (def.Include != null)
        {
            foreach (var inc in def.Include) query = query.Include(inc);
        }

        // CONDITIONS
        foreach (var condition in def.Conditions)
        {
            if (condition.Field.Equals("Status", StringComparison.OrdinalIgnoreCase))
            {
                if (condition.Op == "==") query = query.Where(b => b.Status == condition.Value);
            }
            // ... (resten af dine conditions)
        }

        // EXECUTE
        var entities = await query.ToListAsync();

        // MAP (Her kalder vi en hjælper for at undgå kode-duplikering)
        return MapEntitiesToDictionary(entities, def);
    }

    // NY METODE
    private async Task<List<Dictionary<string, object>>> ExecuteHouseQuery(QueryDefinition def)
    {
        var query = _dbContext.Houses.AsQueryable();

        // Houses er simple (ingen conditions implementeret i dette eksempel, men kan tilføjes)

        var entities = await query.ToListAsync();
        return MapEntitiesToDictionary(entities, def);
    }

    // Specifik handler for Customers (NY!)
    private async Task<List<Dictionary<string, object>>> ExecuteCustomerQuery(QueryDefinition def)
    {
        var query = _dbContext.Customers.AsQueryable();

        // CONDITIONS (Kunder har andre felter end Bookings)
        foreach (var condition in def.Conditions)
        {
            if (condition.Field.Equals("Email", StringComparison.OrdinalIgnoreCase))
            {
                if (condition.Op == "==") query = query.Where(c => c.Email == condition.Value);
            }
            // Tilføj flere customer-specifikke filtre her
        }

        var entities = await query.ToListAsync();
        return MapEntitiesToDictionary(entities, def);
    }

    // Generisk Mapper (Virker for alle typer!)
    private List<Dictionary<string, object>> MapEntitiesToDictionary<T>(List<T> entities, QueryDefinition def)
    {
        var results = new List<Dictionary<string, object>>();
        var props = typeof(T).GetProperties(); // Reflection: Find alle properties på typen T

        foreach (var entity in entities)
        {
            var row = new Dictionary<string, object>();

            // Hvis Select er tom, tag alle properties
            var selectAll = def.Select == null || def.Select.Count == 0;

            foreach (var prop in props)
            {
                // Skal feltet med? (Enten SelectAll, eller navnet findes i listen)
                if (selectAll || def.Select.Contains(prop.Name))
                {
                    var value = prop.GetValue(entity); // Hent værdien dynamisk
                    row[prop.Name] = value;
                }
            }

            // Håndter nested relationer (Lidt sværere med reflection, men vi kan hardcode "Customer.Name" i Booking delen hvis nødvendigt)
            // For nu holder vi denne generiske mapper simpel: Den tager kun direkte felter.

            results.Add(row);
        }
        return results;
    }

    private async Task<List<Dictionary<string, object>>> ExecuteLogQuery(QueryDefinition def)
    {
        var query = _dbContext.Logs.AsQueryable();
        // (Tilføj evt. sortering her hvis du vil være avanceret, ellers hent bare data)
        var entities = await query.ToListAsync();
        return MapEntitiesToDictionary(entities, def);
    }


}
