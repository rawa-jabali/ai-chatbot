namespace ChatBot.Core;

public record IngestRequest(
    string FileName,
    string? ContentType,
    Stream Content,
    string? ChunkerOverride = null
);

public record IngestResult(
    string FileName,
    string Extractor,
    string Chunker,
    int Sections,
    int Chunks,
    bool Embedded
);

public interface IIngestionPipeline
{
    Task<IngestResult> ExecuteAsync(IngestRequest req, CancellationToken ct = default);
}