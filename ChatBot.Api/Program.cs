using Microsoft.Extensions.Options;
using ChatBot.Core;
using ChatBot.Api.Storage;
using ChatBot.Api.Pipeline;

// Embedding (OpenAI)
using OpenAI;
using ChatBot.Indexing.Ai; 

// VectorStore (Qdrant)
using Qdrant.Client;
using ChatBot.Ingestion.Extractors;
using ChatBot.Api.Options;
using ChatBot.Indexing;
using ChatBot.Core.Interfaces;
using ChatBot.Indexing.AI;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();

builder.Services.Configure<OpenAIOptions>(builder.Configuration.GetSection("OpenAI"));
builder.Services.Configure<VectorStoreOptions>(builder.Configuration.GetSection("VectorStore"));

builder.Services.AddSingleton<IFileStorage, LocalFileStorage>();

builder.Services.AddSingleton<ITextExtractor, PlainTextExtractor>();
builder.Services.AddSingleton<ITextExtractor, PdfTextExtractor>();
builder.Services.AddSingleton<ITextExtractor, ExcelTextExtractor>();
builder.Services.AddSingleton<ITextExtractor, WordDocxExtractor>();
builder.Services.AddSingleton<ITextExtractor, PowerPointPptxExtractor>();

builder.Services.AddSingleton<IChunker, ParagraphGreedyChunker>();
builder.Services.AddSingleton<IChunker, SlidingWindowChunker>();

builder.Services.AddSingleton<IRetrievalPipeline, RetrievalPipeline>();
builder.Services.AddSingleton<IAnswerPipeline, AnswerPipeline>();


builder.Services.AddSingleton(sp =>
{
    var o = sp.GetRequiredService<IOptions<OpenAIOptions>>().Value;
    return string.IsNullOrWhiteSpace(o.ApiKey) ? null : new OpenAIClient(o.ApiKey);
});

builder.Services.AddSingleton<IChatService>(sp =>
{
    var o = sp.GetRequiredService<IOptions<OpenAIOptions>>().Value;
    var client = sp.GetService<OpenAIClient>();

    return new OpenAIChatService(client, o.ChatModel);
});

builder.Services.AddSingleton<IEmbeddingService>(sp =>
{
    var o = sp.GetRequiredService<IOptions<OpenAIOptions>>().Value;
    var client = sp.GetService<OpenAIClient>();

    return new OpenAIEmbeddingService(client, o.EmbeddingModel, o.EmbeddingVectorSize);
});

builder.Services.AddSingleton(sp =>
{
    var v = sp.GetRequiredService<IOptions<VectorStoreOptions>>().Value;
    return new VectorCollectionConfig(v.Collection.Name, v.Collection.VectorSize);
});

builder.Services.AddSingleton<IVectorStore>(sp =>
{
    var v = sp.GetRequiredService<IOptions<VectorStoreOptions>>().Value;
    var provider = (v.Provider ?? "qdrant").ToLowerInvariant();

    if (provider == "qdrant")
    {
        var qc = new QdrantClient(new Uri(v.Qdrant.Url));
        return new ChatBot.Indexing.VectorStores.QdrantVectorStore(qc);
    }

    throw new InvalidOperationException($"Unknown VectorStore provider: {provider}");
});

builder.Services.AddSingleton<IExtractorSelector, ExtractorSelector>();
builder.Services.AddSingleton<IChunkerSelector, ChunkerSelector>();
builder.Services.AddSingleton<IIngestionPipeline, IngestionPipeline>();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

// app.MapPost("/files/upload", async (HttpRequest req, IFileStorage storage, CancellationToken ct) =>
// {
//     if (!req.HasFormContentType)
//         return Results.BadRequest(new { ok = false, error = "multipart/form-data required" });

//     var form = await req.ReadFormAsync(ct);
//     var file = form.Files.FirstOrDefault();
//     if (file is null)
//         return Results.BadRequest(new { ok = false, error = "file is required" });

//     var saved = await storage.SaveAsync(file, ct);
//     return Results.Ok(new { ok = true, fileName = saved.FileName });
// });

app.MapPost("/ask", async (AskRequest req, IAnswerPipeline pipeline, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(req.Question))
        return Results.BadRequest(new { ok = false, error = "question is required" });

    var res = await pipeline.AskAsync(req.Question, ct);

    return Results.Ok(new
    {
        ok = true,
        answer = res.Answer,
        usedEmbeddings = res.UsedEmbeddings,
        sources = res.Sources.Select(s => new { s.File, s.ChunkIndex, s.Score }).ToList()
    });
});


app.MapPost("/index/{fileName}", async (
    string fileName,
    IFileStorage storage,
    IIngestionPipeline pipeline,
    IHttpContextAccessor http,
    CancellationToken ct) =>
{
    if (!storage.Exists(fileName))
        return Results.NotFound(new { ok = false, error = "file not found" });

    var chunkerOverride = http.HttpContext?.Request.Query["chunker"].ToString();
    if (string.IsNullOrWhiteSpace(chunkerOverride)) chunkerOverride = null;

    await using var stream = await storage.OpenReadAsync(fileName, ct);

    var result = await pipeline.ExecuteAsync(new IngestRequest(
        FileName: fileName,
        ContentType: null,
        Content: stream,
        ChunkerOverride: chunkerOverride
    ), ct);

    return Results.Ok(new { ok = true, result });
});

app.Run();
