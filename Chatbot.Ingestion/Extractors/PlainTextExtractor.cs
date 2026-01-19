using ChatBot.Core.Interfaces;

namespace ChatBot.Ingestion.Extractors;

public class PlainTextExtractor : ITextExtractor
{
    public bool CanHandle(string fileName, string? contentType)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext is ".txt" or ".md";
    }

    public async Task<ExtractResult> ExtractAsync(Stream file, string fileName, CancellationToken ct)
    {
        using var reader = new StreamReader(file);
        var text = await reader.ReadToEndAsync(ct);

        return new ExtractResult(fileName, new[]
        {
            new ExtractSection(text, new Dictionary<string,string> { ["type"] = "text" })
        });
    }
}