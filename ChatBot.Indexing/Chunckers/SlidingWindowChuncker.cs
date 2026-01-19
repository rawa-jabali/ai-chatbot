
using ChatBot.Core.Interfaces;

namespace ChatBot.Indexing;

public class SlidingWindowChunker : IChunker
{
    public string Name => "sliding_window";

    public bool CanHandle(string fileName, string? contentType, IReadOnlyList<ExtractSection> sections)
        => true;

    public List<Chunk> Chunk(IReadOnlyList<ExtractSection> sections, ChunkingOptions options)
    {
        var max = options.MaxChars;
        var overlap = Math.Max(0, options.OverlapChars);

        var chunks = new List<Chunk>();

        foreach (var section in sections)
        {
            var text = section.Text?.Trim() ?? "";
            if (text.Length == 0) continue;

            int start = 0;
            while (start < text.Length)
            {
                var len = Math.Min(max, text.Length - start);
                var part = text.Substring(start, len);

                chunks.Add(new Chunk(part, new Dictionary<string,string>(section.Metadata)));

                if (start + len >= text.Length) break;
                start = Math.Max(0, start + len - overlap);
            }
        }

        return chunks;
    }
}
