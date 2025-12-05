using System.Text;

namespace Applicate.Domain;

public class ExportService
{
    public byte[] ConvertToCsv(List<Dictionary<string, object>> data)
    {
        if (data == null || data.Count == 0) return Array.Empty<byte>();

        var sb = new StringBuilder();

        // 1. Headers (Tag keys fra første række)
        var headers = data[0].Keys.ToList();
        sb.AppendLine(string.Join(";", headers)); // Semikolon er bedst til Excel i DK

        // 2. Rows
        foreach (var row in data)
        {
            var values = headers.Select(h =>
            {
                var val = row.ContainsKey(h) ? row[h]?.ToString() ?? "" : "";

                // Escape hvis der er semikolon eller linjeskift i teksten
                if (val.Contains(";") || val.Contains("\n"))
                {
                    val = $"\"{val.Replace("\"", "\"\"")}\"";
                }
                return val;
            });

            sb.AppendLine(string.Join(";", values));
        }

        // Returner som bytes (med UTF8 BOM så Excel forstår æøå)
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }
}