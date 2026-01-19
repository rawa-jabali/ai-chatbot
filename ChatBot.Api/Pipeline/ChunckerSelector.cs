using ChatBot.Core;
using ChatBot.Core.Interfaces;

namespace ChatBot.Api.Pipeline;

public class ChunkerSelector : IChunkerSelector
{
    private readonly IEnumerable<IChunker> _chunkers;
    private readonly IConfiguration _config;

    public ChunkerSelector(IEnumerable<IChunker> chunkers, IConfiguration config)
    {
        _chunkers = chunkers;
        _config = config;
    }

    public IChunker Select(string fileName, string? contentType, IReadOnlyList<ExtractSection> sections, string? overrideName = null)
    {
        // 1) override من المستخدم
        if (!string.IsNullOrWhiteSpace(overrideName))
        {
            var chosen = _chunkers.FirstOrDefault(c => c.Name.Equals(overrideName, StringComparison.OrdinalIgnoreCase));
            if (chosen is null) throw new InvalidOperationException($"Unknown chunker: {overrideName}");
            return chosen;
        }

        var ext = Path.GetExtension(fileName).ToLowerInvariant();

        if (ext == ".xlsx")
            return FindOrDefault("excel_rows", "paragraph_greedy");

        if (ext == ".pptx")
            return FindOrDefault("paragraph_greedy", "sliding_window");

        if (ext == ".pdf")
            return FindOrDefault("paragraph_greedy", "sliding_window");

        if (ext is ".docx" or ".txt" or ".md")
            return FindOrDefault("paragraph_greedy", "sliding_window");

        var def = _config["Chunking:Default"] ?? "paragraph_greedy";
        return FindOrDefault(def, "paragraph_greedy");
    }

    private IChunker FindOrDefault(string preferred, string fallback)
        => _chunkers.FirstOrDefault(c => c.Name.Equals(preferred, StringComparison.OrdinalIgnoreCase))
           ?? _chunkers.First(c => c.Name.Equals(fallback, StringComparison.OrdinalIgnoreCase));
}