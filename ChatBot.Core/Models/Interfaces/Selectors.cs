namespace ChatBot.Core.Interfaces;

public interface IExtractorSelector
{
    ITextExtractor Select(string fileName, string? contentType);
}

public interface IChunkerSelector
{
    IChunker Select(string fileName, string? contentType, IReadOnlyList<ExtractSection> sections, string? overrideName = null);
}