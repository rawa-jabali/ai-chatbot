namespace ChatBot.Core;

public record VectorCollectionConfig(string Name, int VectorSize);

public record VectorUpsertItem(
    ulong Id,
    float[] Vector,
    Dictionary<string, object> Payload 
);

public record VectorHit(
    Dictionary<string, object> Payload,
    float Score
);

public interface IVectorStore
{
    Task EnsureCollectionAsync(VectorCollectionConfig cfg, CancellationToken ct = default);

    Task UpsertAsync(VectorCollectionConfig cfg, IReadOnlyList<VectorUpsertItem> items, CancellationToken ct = default);

    Task<IReadOnlyList<VectorHit>> SearchAsync(VectorCollectionConfig cfg, float[] queryVector, int limit, CancellationToken ct = default);

    Task<IReadOnlyList<VectorHit>> ScrollAsync(VectorCollectionConfig cfg, int limit, CancellationToken ct = default);
}

