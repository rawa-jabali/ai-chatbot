using ChatBot.Core;
using ChatBot.Core.Interfaces;
using OpenAI;

namespace ChatBot.Indexing.Ai;

public class OpenAIEmbeddingService : IEmbeddingService
{
    private readonly OpenAIClient _client;
    private readonly string _model;

    public int VectorSize { get; }

    public OpenAIEmbeddingService(OpenAIClient client, string model, int vectorSize)
    {
        _client = client;
        _model = model;
        VectorSize = vectorSize;
    }

    public async Task<float[]> EmbedAsync(string input, CancellationToken ct = default)
    {
        var embeddings = _client.GetEmbeddingClient(_model);
        var res = await embeddings.GenerateEmbeddingAsync(input, cancellationToken: ct);

        return res.Value.ToFloats().ToArray();
    }
}