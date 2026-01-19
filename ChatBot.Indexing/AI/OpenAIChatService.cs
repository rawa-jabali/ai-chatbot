using System.Text;
using ChatBot.Core.Interfaces;
using OpenAI;
using OpenAI.Chat;

namespace ChatBot.Indexing.AI;

public class OpenAIChatService : IChatService
{
    private readonly OpenAIClient _client;
    private readonly string _chatModel;

    public OpenAIChatService(OpenAIClient client, string chatModel)
    {
        _client = client;
        _chatModel = chatModel;
    }

    public async Task<ChatAnswer> AnswerAsync(string question, IReadOnlyList<SourceSnippet> sources, CancellationToken ct = default)
    {
        var context = BuildContext(sources);

        var system = """
You are an internal company assistant.
Rules:
- Answer ONLY using the provided sources.
- If the answer is not in the sources, say: "I couldn't find this in the available documents."
- Keep it short and clear.
- At the end, list sources as: (file, chunk).
""";

        var chat = _client.GetChatClient(_chatModel);
        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(system),
            new UserChatMessage($"Question: {question}\n\nSOURCES:\n{context}")
        };

        var resp = await chat.CompleteChatAsync(messages, new ChatCompletionOptions { Temperature = 0.2f }, ct);
        var answerText = resp.Value.Content[0].Text;

        var srcList = sources
            .Select(s => (s.File, s.ChunkIndex, s.Score))
            .ToList();

        return new ChatAnswer(answerText, srcList);
    }

    private static string BuildContext(IReadOnlyList<SourceSnippet> hits)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < hits.Count; i++)
        {
            sb.AppendLine($"Source {i + 1} | file: {hits[i].File} | chunk: {hits[i].ChunkIndex} | score: {hits[i].Score}");
            sb.AppendLine(hits[i].Text);
            sb.AppendLine("\n---\n");
        }
        return sb.ToString();
    }
}