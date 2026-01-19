using ChatBot.Core.Interfaces;
using UglyToad.PdfPig;

namespace ChatBot.Ingestion.Extractors;

public class PdfTextExtractor : ITextExtractor
{
    public bool CanHandle(string fileName, string? contentType)
        => Path.GetExtension(fileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase);

    public Task<ExtractResult> ExtractAsync(Stream file, string fileName, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        file.CopyTo(ms);
        ms.Position = 0;

        using var doc = PdfDocument.Open(ms);

        var sections = new List<ExtractSection>();
        foreach (var page in doc.GetPages())
        {
            var text = page.Text?.Trim();
            if (string.IsNullOrWhiteSpace(text)) continue;

            sections.Add(new ExtractSection(text, new Dictionary<string,string>
            {
                ["type"] = "pdf",
                ["page"] = page.Number.ToString()
            }));
        }

        return Task.FromResult(new ExtractResult(fileName, sections));
    }
}