namespace ChatBot.Core.Interfaces;

public record SourceSnippet(string File, int ChunkIndex, float Score, string Text);

public record ChatAnswer(string Answer, IReadOnlyList<(string file, int chunkIndex, float score)> Sources);

public interface IChatService
{
    Task<ChatAnswer> AnswerAsync(string question, IReadOnlyList<SourceSnippet> sources, CancellationToken ct = default);
}