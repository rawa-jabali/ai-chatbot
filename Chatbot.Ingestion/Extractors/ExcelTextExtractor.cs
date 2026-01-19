using System.Text;
using ChatBot.Core.Interfaces;
using ClosedXML.Excel;

namespace ChatBot.Ingestion.Extractors;

public class ExcelTextExtractor : ITextExtractor
{
    public bool CanHandle(string fileName, string? contentType)
        => Path.GetExtension(fileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase);

    public Task<ExtractResult> ExtractAsync(Stream file, string fileName, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        file.CopyTo(ms);
        ms.Position = 0;

        using var wb = new XLWorkbook(ms);

        var sections = new List<ExtractSection>();
        foreach (var ws in wb.Worksheets)
        {
            var used = ws.RangeUsed();
            if (used is null) continue;

            var sb = new StringBuilder();
            sb.AppendLine($"Sheet: {ws.Name}");

            foreach (var row in used.Rows())
            {
                var values = row.Cells().Select(c => c.GetValue<string>()).ToArray();
                if (values.All(string.IsNullOrWhiteSpace)) continue;
                sb.AppendLine(string.Join(" | ", values));
            }

            var text = sb.ToString().Trim();
            if (text.Length == 0) continue;

            sections.Add(new ExtractSection(text, new Dictionary<string,string>
            {
                ["type"] = "excel",
                ["sheet"] = ws.Name
            }));
        }

        return Task.FromResult(new ExtractResult(fileName, sections));
    }
}