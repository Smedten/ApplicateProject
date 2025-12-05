using Applicate.Domain;
using Applicate.Domain.Data; // Vigtig: For at kunne se databasen
using Applicate.Domain.Models;
using Spectre.Console;

namespace Applicate.Cli;

public class CliApp
{
    private readonly ResourceService _resourceService;
    private readonly ActionService _actionService;
    private readonly QueryExecutor _queryExecutor;

    public CliApp(ResourceService resourceService, ActionService actionService, QueryExecutor queryExecutor)
    {
        _resourceService = resourceService;
        _actionService = actionService;
        _queryExecutor = queryExecutor;
    }

    public async Task RunAsync(string[] args)
    {
        await AnsiConsole.Status()
            .StartAsync("Indlæser specs...", async ctx =>
            {
                await _resourceService.LoadFromDirectoryAsync(Path.Combine(AppContext.BaseDirectory, "specs"));
            });

        AnsiConsole.Write(new FigletText("APPLICATE").Color(Color.Cyan1));
        AnsiConsole.MarkupLine("[grey]Type 'exit' to quit[/]");
        AnsiConsole.WriteLine();

        while (true)
        {
            var input = AnsiConsole.Ask<string>("[green]app>[/]");
            var cmd = CliParser.Parse(input);
            if (cmd == null) continue;

            if (cmd.Subject == "exit") break;

            try
            {
                switch (cmd.Subject)
                {
                    case "resource":
                        await HandleResourceCommand(cmd);
                        break;
                    default:
                        AnsiConsole.MarkupLine($"[red]Ukendt kommando: {cmd.Subject}[/]");
                        break;
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Fejl: {ex.Message}[/]");
            }
            AnsiConsole.WriteLine();
        }
    }

    private async Task HandleResourceCommand(ParsedCommand cmd)
    {
        switch (cmd.Verb)
        {
            case "list":
                ListResources(cmd);
                break;
            case "describe":
                DescribeResource(cmd);
                break;
            case "run":
                await HandleRunCommand(cmd);
                break;
            default:
                AnsiConsole.MarkupLine($"[yellow]Ukendt handling '{cmd.Verb}' for resource.[/]");
                break;
        }
    }

    private void ListResources(ParsedCommand cmd)
    {
        // TJEK: Vil brugeren se DATA for en specifik tabel?
        // Kommando: resource list Booking --data
        if (cmd.Options.ContainsKey("data") && cmd.Positionals.Count > 0)
        {
            var resourceName = cmd.Positionals[0];

            // Lige nu hardcoder vi 'Booking' tjekket, fordi vi kun har én tabel i koden.
            // I fremtiden skal dette være dynamisk.
            //if (resourceName.Equals("Booking", StringComparison.OrdinalIgnoreCase))
            //{
            //    ShowBookingData();
            //    return; // Stop her, så vi ikke viser listen af specs
            //}
            //else
            //{
            //    AnsiConsole.MarkupLine($"[red]Kan kun vise data for 'Booking' lige nu.[/]");
            //    return;
            //}
        }

        // --- Standard logik (Viser Specs) ---
        var resources = _resourceService.GetAllResources();

        if (cmd.Options.TryGetValue("kind", out var kindStr))
        {
            if (Enum.TryParse<ResourceKind>(kindStr, true, out var kindEnum))
                resources = resources.Where(r => r.Kind == kindEnum).ToList();
        }

        var table = new Table();
        table.AddColumn("Name");
        table.AddColumn("Kind");
        table.AddColumn("Fields");

        foreach (var res in resources)
            table.AddRow(res.Name, res.Kind.ToString(), res.Fields.Count.ToString());

        AnsiConsole.Write(table);
    }

    // Hjælpe-metode til at vise data fra databasen
    //private void ShowBookingData()
    //{
    //    var bookings = _dbContext.Bookings.ToList(); // Hent alt fra DB

    //    var table = new Table();
    //    table.AddColumn("Id");
    //    table.AddColumn("Status");
    //    table.AddColumn("Pris");
    //    table.AddColumn("Start Dato");

    //    foreach (var b in bookings)
    //    {
    //        // Farv status for bedre overblik
    //        var statusColor = b.Status == "Confirmed" ? "green" : "yellow";

    //        table.AddRow(
    //            b.Id.ToString(),
    //            $"[{statusColor}]{b.Status}[/]", 
    //            b.TotalPrice.ToString("N2"),
    //            b.StartDate.ToShortDateString()
    //        );
    //    }

    //    AnsiConsole.Write(table);
    //}

    private async Task HandleRunCommand(ParsedCommand cmd)
    {
        if (cmd.Positionals.Count < 1)
        {
            AnsiConsole.MarkupLine("[red]Angiv navnet på en resource.[/]");
            return;
        }

        string resourceName = cmd.Positionals[0];
        var resource = _resourceService.GetResource(resourceName);

        if (resource == null)
        {
            AnsiConsole.MarkupLine($"[red]Ukendt resource: {resourceName}[/]");
            return;
        }

        // SCENARIO 1: Det er en QUERY (Rapport) -> Kør QueryExecutor
        if (resource.Kind == ResourceKind.Query)
        {
            AnsiConsole.MarkupLine($"Kører rapporten [cyan]{resourceName}[/]...");
            try
            {
                var results = await _queryExecutor.ExecuteQueryAsync(resourceName);

                // Vis dynamisk tabel
                if (results.Count > 0)
                {
                    var table = new Table();
                    // Opret kolonner automatisk ud fra første række
                    foreach (var key in results[0].Keys) table.AddColumn(key);

                    foreach (var row in results)
                    {
                        // Konverter værdier til strings
                        var values = row.Values.Select(v => v?.ToString() ?? "").ToArray();
                        table.AddRow(values);
                    }
                    AnsiConsole.Write(table);
                }
                else
                {
                    AnsiConsole.MarkupLine("[yellow]Ingen resultater fundet.[/]");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Query fejl: {ex.Message}[/]");
            }
        }
        // SCENARIO 2: Det er en ACTION (Table) -> Kør ActionService
        else if (resource.Kind == ResourceKind.Table)
        {
            if (cmd.Positionals.Count < 2)
            {
                AnsiConsole.MarkupLine("[red]Du skal angive en action, f.eks: run Booking Confirm[/]");
                return;
            }
            string actionName = cmd.Positionals[1];
            await _actionService.ExecuteActionAsync(resourceName, actionName, cmd.Options);
            AnsiConsole.MarkupLine($"[green]Handlingen '{actionName}' er udført![/]");
        }
    }

    private async void RunAction(ParsedCommand cmd)
    {
        if (cmd.Positionals.Count != 2)
        {
            AnsiConsole.MarkupLine("[red]Brug: resource run <Resource> <Action> --param value[/]");
            return;
        }

        string resourceName = cmd.Positionals[0];
        string actionName = cmd.Positionals[1];

        AnsiConsole.MarkupLine($"Kører [cyan]{actionName}[/] på [yellow]{resourceName}[/]...");

        try
        {
            await _actionService.ExecuteActionAsync(resourceName, actionName, cmd.Options);
            AnsiConsole.MarkupLine($"[green]Handlingen er udført[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Fejl under kørsel: {ex.Message}[/]");
        }
    }

    private void DescribeResource(ParsedCommand cmd)
    {
        // ... (Din eksisterende kode) ...
        if (cmd.Positionals.Count == 0) return;
        var resource = _resourceService.GetResource(cmd.Positionals[0]);
        if (resource == null) return;

        var tree = new Tree(resource.Name);
        var fields = tree.AddNode("Fields");
        foreach (var f in resource.Fields) fields.AddNode(f.Name);

        if (resource.Actions.Any())
        {
            var actions = tree.AddNode("Actions");
            foreach (var a in resource.Actions) actions.AddNode(a.Name);
        }
        AnsiConsole.Write(tree);
    }
}