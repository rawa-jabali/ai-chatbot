using ChatBot.Core;
using ChatBot.Core.Interfaces;

namespace ChatBot.Api.Pipeline;

public class IngestionPipeline : IIngestionPipeline
{
    private readonly IExtractorSelector _extractorSelector;
    private readonly IChunkerSelector _chunkerSelector;
    private readonly IEmbeddingService _embedding;
    private readonly IVectorStore _store;
    private readonly VectorCollectionConfig _collectionCfg;
    private readonly IConfiguration _config;

    public IngestionPipeline(
        IExtractorSelector extractorSelector,
        IChunkerSelector chunkerSelector,
        IEmbeddingService embedding,
        IVectorStore store,
        VectorCollectionConfig collectionCfg,
        IConfiguration config)
    {
        _extractorSelector = extractorSelector;
        _chunkerSelector = chunkerSelector;
        _embedding = embedding;
        _store = store;
        _collectionCfg = collectionCfg;
        _config = config;
    }

    public async Task<IngestResult> ExecuteAsync(IngestRequest req, CancellationToken ct = default)
    {
        // 1) extractor
        var extractor = _extractorSelector.Select(req.FileName, req.ContentType);

        // 2) extract
        var extracted = await extractor.ExtractAsync(req.Content, req.FileName, ct);
        if (extracted.Sections.Count == 0)
            throw new InvalidOperationException("No text extracted. Scanned documents require OCR.");

        // 3) chunker (auto by file type)
        var chunker = _chunkerSelector.Select(req.FileName, req.ContentType, extracted.Sections, req.ChunkerOverride);

        var maxChars = int.Parse(_config["Chunking:MaxChars"] ?? "900");
        var overlap = int.Parse(_config["Chunking:OverlapChars"] ?? "0");

        var chunks = chunker.Chunk(extracted.Sections, new ChunkingOptions(MaxChars: maxChars, OverlapChars: overlap));

        // 4) validate vector sizes
        if (_collectionCfg.VectorSize != _embedding.VectorSize)
            throw new InvalidOperationException($"VectorSize mismatch: Collection={_collectionCfg.VectorSize}, Embedding={_embedding.VectorSize}");

        // 5) ensure collection
        await _store.EnsureCollectionAsync(_collectionCfg, ct);

        // 6) embed + upsert
        ulong idBase = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var items = new List<VectorUpsertItem>(chunks.Count);

        for (int i = 0; i < chunks.Count; i++)
        {
            var c = chunks[i];
            var vec = await _embedding.EmbedAsync(c.Text, ct);

            var payload = new Dictionary<string, object>
            {
                ["file"] = req.FileName,
                ["chunkIndex"] = i,
                ["text"] = c.Text
            };

            foreach (var kv in c.Metadata)
                payload[kv.Key] = kv.Value;

            items.Add(new VectorUpsertItem(idBase + (ulong)i, vec, payload));
        }

        await _store.UpsertAsync(_collectionCfg, items, ct);

        return new IngestResult(
            FileName: req.FileName,
            Extractor: extractor.GetType().Name,
            Chunker: chunker.Name,
            Sections: extracted.Sections.Count,
            Chunks: chunks.Count,
            Embedded: _embedding is not null
        );
    }
}