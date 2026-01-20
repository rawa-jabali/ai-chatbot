using System.Text;
using ChatBot.Core;
using ChatBot.Core.Interfaces;

namespace ChatBot.Api.Pipeline;

public class AnswerPipeline : IAnswerPipeline
{
    private readonly IRetrievalPipeline _retrieval;
    private readonly IChatService _chat;

    public AnswerPipeline(IRetrievalPipeline retrieval, IChatService chat)
    {
        _retrieval = retrieval;
        _chat = chat;
    }

    public async Task<AskResult> AskAsync(string question, CancellationToken ct = default)
    {
        var (hits, usedEmbeddings) = await _retrieval.RetrieveAsync(question, ct);

        if (hits.Count == 0)
        {
            return new AskResult(
                Answer: "I couldn't find this in the available documents.",
                Sources: hits,
                UsedEmbeddings: usedEmbeddings,
                UsedLlm: false
            );
        }

        var answer = await _chat.AnswerAsync(question, hits.Select(h => new SourceSnippet(h.File, h.ChunkIndex, h.Score, h.Text)).ToList(), ct);

        return new AskResult(
            Answer: answer.Answer,
            Sources: hits,
            UsedEmbeddings: usedEmbeddings,
            UsedLlm: answer.Answer != null && answer.Answer.Length > 0
        );
    }
}