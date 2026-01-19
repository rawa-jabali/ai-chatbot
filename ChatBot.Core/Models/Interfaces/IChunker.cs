
namespace ChatBot.Core.Interfaces;

public interface IChunker
{
    string Name { get; }           
    bool CanHandle(string fileName, string? contentType, IReadOnlyList<ExtractSection> sections);
    List<Chunk> Chunk(IReadOnlyList<ExtractSection> sections, ChunkingOptions options);
}

public record ChunkingOptions(
    int MaxChars = 900,
    int OverlapChars = 0,
    string? Mode = null // Optional: "paragraph", "fixed",
);
