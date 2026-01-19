namespace ChatBot.Core.Interfaces;
public interface ITextExtractor
{
    bool CanHandle(string fileName, string? contentType);
    Task<ExtractResult> ExtractAsync(Stream file, string fileName, CancellationToken ct);
}

public record ExtractResult(string FileName, IReadOnlyList<ExtractSection> Sections);

public record ExtractSection(string Text, Dictionary<string, string> Metadata);

public record Chunk(string Text, Dictionary<string, string> Metadata);