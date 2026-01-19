namespace ChatBot.Core;

public record AskRequest(string Question);

public record AskHit(string File, int ChunkIndex, float Score, string Text);

public record AskResult(
    string Answer,
    IReadOnlyList<AskHit> Sources,
    bool UsedEmbeddings,
    bool UsedLlm
);

public interface IRetrievalPipeline
{
    Task<(IReadOnlyList<AskHit> hits, bool usedEmbeddings)> RetrieveAsync(string question, CancellationToken ct = default);
}

public interface IAnswerPipeline
{
    Task<AskResult> AskAsync(string question, CancellationToken ct = default);
}
