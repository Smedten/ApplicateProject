namespace Applicate.Domain.Models;

public record ParsedCommand(
    string Subject,                    // f.eks. "resource"
    string Verb,                       // f.eks. "list" eller "run"
    List<string> Positionals,          // f.eks. ["Booking", "Confirm"] (ting uden --)
    Dictionary<string, string> Options // f.eks. { "kind": "Table", "dry-run": "true" }
);