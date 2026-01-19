using System.Text;
using ChatBot.Core.Interfaces;

namespace ChatBot.Indexing;

public class ParagraphGreedyChunker : IChunker
{
    public string Name => "paragraph_greedy";

    public bool CanHandle(string fileName, string? contentType, IReadOnlyList<ExtractSection> sections)
        => true; 

    public List<Chunk> Chunk(IReadOnlyList<ExtractSection> sections, ChunkingOptions options)
    {
        var maxChars = options.MaxChars;
        var chunks = new List<Chunk>();

        foreach (var section in sections)
        {
            var parts = section.Text
                .Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .ToList();

            var sb = new StringBuilder();

            void flush()
            {
                if (sb.Length == 0) return;
                chunks.Add(new Chunk(sb.ToString(), new Dictionary<string, string>(section.Metadata)));
                sb.Clear();
            }

            foreach (var p in parts)
            {
                if (sb.Length + p.Length + 2 > maxChars)
                    flush();

                if (sb.Length > 0) sb.Append("\n");
                sb.Append(p);
            }

            flush();
        }

        return chunks;
    }
}