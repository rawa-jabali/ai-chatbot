using System.Text;
using ChatBot.Core.Interfaces;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace ChatBot.Ingestion.Extractors;

public class WordDocxExtractor : ITextExtractor
{
    public bool CanHandle(string fileName, string? contentType)
        => Path.GetExtension(fileName)
               .Equals(".docx", StringComparison.OrdinalIgnoreCase);

    public Task<ExtractResult> ExtractAsync(
        Stream file,
        string fileName,
        CancellationToken ct)
    {
        using var ms = new MemoryStream();
        file.CopyTo(ms);
        ms.Position = 0;

        using var doc = WordprocessingDocument.Open(ms, false);
        var body = doc.MainDocumentPart?.Document?.Body;

        if (body is null)
            return Task.FromResult(
                new ExtractResult(fileName, Array.Empty<ExtractSection>()));

        var sb = new StringBuilder();

        foreach (var para in body.Descendants<Paragraph>())
        {
            var text = para.InnerText?.Trim();
            if (!string.IsNullOrWhiteSpace(text))
                sb.AppendLine(text);
        }

        var allText = sb.ToString().Trim();
        if (allText.Length == 0)
            return Task.FromResult(
                new ExtractResult(fileName, Array.Empty<ExtractSection>()));

        return Task.FromResult(new ExtractResult(
            fileName,
            new[]
            {
                new ExtractSection(
                    allText,
                    new Dictionary<string, string>
                    {
                        ["type"] = "docx"
                    })
            }));
    }
}