namespace ChatBot.Core.Interfaces;

public interface IEmbeddingService
{
    int VectorSize { get; }
    Task<float[]> EmbedAsync(string input, CancellationToken ct = default);
}

