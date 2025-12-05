using Applicate.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Applicate.Domain;

public class ResourceService
{
    private readonly Dictionary<string, ResourceDef> _resources = new(StringComparer.OrdinalIgnoreCase);
    public ResourceService() { }

    public List<ResourceDef> GetAllResources(){
        return _resources.Values.ToList();
    }

    public ResourceDef GetResource(string name)
    {
        return _resources[name];
    }

    public async Task LoadFromDirectoryAsync(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            throw new DirectoryNotFoundException($"Kunne ikke finde specs mappen: {directoryPath}");
        }

        var files = Directory.GetFiles(directoryPath, "*.json");

        foreach (var file in files) 
        {
            try
            {
                var resource = await LoadSingleFileAsync(file);
                if (resource != null)
                {
                    _resources[resource.Name] = resource;
                    Console.WriteLine($"[INFO] Loaded spec: {resource.Name} ({resource.Kind})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Kunne ikke indlæse filen {file}: {ex.Message}");
            }
        } 
    }

    // Din originale logik - nu som en private helper metode
    private async Task<ResourceDef?> LoadSingleFileAsync(string filePath)
    {
        var jsonContent = await File.ReadAllTextAsync(filePath);

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            // Denne dims tillader kommentarer i JSON (rart til specs)
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        return JsonSerializer.Deserialize<ResourceDef>(jsonContent, options);
    }
}
