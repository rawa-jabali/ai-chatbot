using ChatBot.Core;
using ChatBot.Core.Interfaces;

namespace ChatBot.Api.Pipeline;

public class ExtractorSelector : IExtractorSelector
{
    private readonly IEnumerable<ITextExtractor> _extractors;

    public ExtractorSelector(IEnumerable<ITextExtractor> extractors)
    {
        _extractors = extractors;
    }

    public ITextExtractor Select(string fileName, string? contentType)
    {
        var ex = _extractors.FirstOrDefault(e => e.CanHandle(fileName, contentType));
        if (ex is null)
            throw new InvalidOperationException($"No extractor found for: {fileName}");
        return ex;
    }
}