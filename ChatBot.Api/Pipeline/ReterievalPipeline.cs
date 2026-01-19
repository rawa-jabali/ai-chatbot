using ChatBot.Core;
using ChatBot.Core.Interfaces;

namespace ChatBot.Api.Pipeline;

public class RetrievalPipeline : IRetrievalPipeline
{
    private readonly IEmbeddingService _embedding;
    private readonly IVectorStore _store;
    private readonly VectorCollectionConfig _collectionCfg;

    public RetrievalPipeline(IEmbeddingService embedding, IVectorStore store, VectorCollectionConfig collectionCfg)
    {
        _embedding = embedding;
        _store = store;
        _collectionCfg = collectionCfg;
    }

    public async Task<(IReadOnlyList<AskHit> hits, bool usedEmbeddings)> RetrieveAsync(string question, CancellationToken ct = default)
    {
        await _store.EnsureCollectionAsync(_collectionCfg, ct);

        var hasRealEmbeddings = !IsAllZeroVector(await _embedding.EmbedAsync("healthcheck", ct));

        if (!hasRealEmbeddings)
        {
            var scroll = await _store.ScrollAsync(_collectionCfg, limit: 5, ct);
            var hits = scroll.Select(ToAskHit).ToList();
            return (hits, usedEmbeddings: false);
        }

        var qVec = await _embedding.EmbedAsync(question, ct);
        var found = await _store.SearchAsync(_collectionCfg, qVec, limit: 5, ct);
        var mapped = found.Select(ToAskHit).ToList();
        return (mapped, usedEmbeddings: true);
    }

    private static AskHit ToAskHit(VectorHit h)
    {
        var p = h.Payload;

        string file = p.TryGetValue("file", out var fv) ? fv?.ToString() ?? "unknown" : "unknown";
        int chunkIndex = p.TryGetValue("chunkIndex", out var cv) ? SafeToInt(cv) : 0;
        string text = p.TryGetValue("text", out var tv) ? tv?.ToString() ?? "" : "";

        return new AskHit(file, chunkIndex, h.Score, text);
    }

    private static int SafeToInt(object? v)
    {
        if (v is null) return 0;
        if (v is int i) return i;
        if (v is long l) return (int)l;
        if (int.TryParse(v.ToString(), out var x)) return x;
        return 0;
    }

    private static bool IsAllZeroVector(float[] v)
    {
        for (int i = 0; i < v.Length; i++)
            if (Math.Abs(v[i]) > 1e-12) return false;
        return true;
    }
}