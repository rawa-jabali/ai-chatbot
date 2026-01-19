using ChatBot.Core;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace ChatBot.Indexing.VectorStores;

public class QdrantVectorStore : IVectorStore
{
    private readonly QdrantClient _qdrant;

    public QdrantVectorStore(QdrantClient qdrant)
    {
        _qdrant = qdrant;
    }

    public async Task EnsureCollectionAsync(VectorCollectionConfig cfg, CancellationToken ct = default)
    {
        var exists = await _qdrant.CollectionExistsAsync(cfg.Name);
        if (!exists)
        {
            await _qdrant.CreateCollectionAsync(cfg.Name, new VectorParams
            {
                Size = (uint)cfg.VectorSize,
                Distance = Distance.Cosine
            });
        }
    }

    public async Task UpsertAsync(VectorCollectionConfig cfg, IReadOnlyList<VectorUpsertItem> items, CancellationToken ct = default)
    {
        foreach (var item in items)
        {
            var payload = new Dictionary<string, Value>();

            foreach (var kv in item.Payload)
            {
                payload[kv.Key] = kv.Value switch
                {
                    string s => new Value { StringValue = s },
                    int i => new Value { IntegerValue = i },
                    long l => new Value { IntegerValue = l },
                    bool b => new Value { BoolValue = b },
                    float f => new Value { DoubleValue = f },
                    double d => new Value { DoubleValue = d },
                    _ => new Value { StringValue = kv.Value?.ToString() ?? "" }
                };
            }

            await _qdrant.UpsertAsync(
                collectionName: cfg.Name,
                points: new[]
                {
                    new PointStruct
                    {
                        Id = new PointId { Num = item.Id },
                        Vectors = item.Vector,
                        Payload = { payload }
                    }
                }
            );
        }
    }

    public async Task<IReadOnlyList<VectorHit>> SearchAsync(VectorCollectionConfig cfg, float[] queryVector, int limit, CancellationToken ct = default)
    {
        var results = await _qdrant.SearchAsync(
            collectionName: cfg.Name,
            vector: queryVector,
            limit: (ulong)limit
        );

        return results.Select(sp => new VectorHit(
            Payload: sp.Payload.ToDictionary(k => k.Key, v => (object)ValueToObject(v.Value)),
            Score: sp.Score
        )).ToList();
    }

    public async Task<IReadOnlyList<VectorHit>> ScrollAsync(VectorCollectionConfig cfg, int limit, CancellationToken ct = default)
    {
        var scroll = await _qdrant.ScrollAsync(
            collectionName: cfg.Name,
            limit: (uint)limit
        );

        var list = scroll.Result?.ToList() ?? new List<RetrievedPoint>();

        return list.Select(rp => new VectorHit(
            Payload: rp.Payload.ToDictionary(k => k.Key, v => (object)ValueToObject(v.Value)),
            Score: 1.0f
        )).ToList();
    }

    private static object ValueToObject(Value v)
    {
        return v.KindCase switch
        {
            Value.KindOneofCase.StringValue => v.StringValue,
            Value.KindOneofCase.IntegerValue => v.IntegerValue,
            Value.KindOneofCase.DoubleValue => v.DoubleValue,
            Value.KindOneofCase.BoolValue => v.BoolValue,
            _ => v.ToString()
        };
    }
}