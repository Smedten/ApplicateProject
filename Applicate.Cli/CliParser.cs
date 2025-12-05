using Applicate.Domain.Models;

namespace Applicate.Cli;

public class CliParser
{
    public static ParsedCommand? Parse(string input)
    {
        // 1. Sanity Check: Er der overhovedet input?
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        // 2. Tokenizing: Split ved mellemrum, men fjern tomme entries (fixer dobbelte mellemrum)
        var parts = input.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0) return null;

        // 3. Uddrag Subject og Verb sikkert
        string subject = parts[0].ToLowerInvariant();

        // Hvis der kun er 1 ord (f.eks. "exit"), så er verb tomt
        string verb = parts.Length > 1 ? parts[1].ToLowerInvariant() : string.Empty;

        var positionals = new List<string>();
        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // 4. Loop gennem resten (fra index 2)
        for (int i = 2; i < parts.Length; i++)
        {
            var token = parts[i];

            if (token.StartsWith("--"))
            {
                // Vi har en option! To formater mulige:
                // A: --kind=Table
                // B: --kind Table

                // Fjern "--" fra starten
                var cleanToken = token.Substring(2);

                if (cleanToken.Contains('='))
                {
                    // FORMAT A: --key=value
                    var kv = cleanToken.Split('=', 2); // Split kun ved første lighedstegn
                    options[kv[0]] = kv[1];
                }
                else
                {
                    // FORMAT B: --key value
                    // Vi skal tjekke om der ER et næste ord, og at det ikke er en ny option
                    if (i + 1 < parts.Length && !parts[i + 1].StartsWith("--"))
                    {
                        options[cleanToken] = parts[i + 1];
                        i++; // VIGTIGT: Vi hopper over næste ord, da vi lige har "spist" det som værdi
                    }
                    else
                    {
                        // Edge case: En flag option uden værdi (f.eks. --force)
                        // Vi sætter den bare til "true"
                        options[cleanToken] = "true";
                    }
                }
            }
            else
            {
                // Det er bare et almindeligt argument (Positional)
                positionals.Add(token);
            }
        }

        return new ParsedCommand(subject, verb, positionals, options);
    }
}