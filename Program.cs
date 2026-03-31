
// Program.cs
//     │
//     ├── Registers all services in DI container
//     ├── Calls RagService.IndexDocumentAsync() on startup
//     └── Defines /ask route → delegates to RagService

// RagService.cs
//     ├── IndexDocumentAsync() → orchestrates indexing pipeline
//     └── AskAsync()          → orchestrates query pipeline

using RAGDemo.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// // manually creating every object
// var pdfService = new PdfService();
// var chunkService = new ChunkService();
// var embeddingService = new EmbeddingService();
// var vectorDb = new VectorDbService();
// var groqService = new GroqService();

// Register all services with DI container
//Now the DI container owns the creation and lifetime of every service. You never write `new ServiceName()` again.
builder.Services.AddHttpClient();
builder.Services.AddSingleton<PdfService>();
builder.Services.AddSingleton<ChunkService>();
builder.Services.AddSingleton<FileHashService>();
builder.Services.AddSingleton<EmbeddingService>();
builder.Services.AddSingleton<VectorDbService>();
builder.Services.AddSingleton<GroqService>();
builder.Services.AddSingleton<RagService>();

var app = builder.Build();
app.UseCors();

// Indexing runs once on startup via RagService
var ragService = app.Services.GetRequiredService<RagService>();
await ragService.IndexDocumentAsync("Data/documents.pdf");
// API — Program.cs only defines the route now
app.MapPost("/ask", async (QuestionRequest request, RagService rag) =>
{
    var answer = await rag.AskAsync(request.Question, request.ConversationId);
    return Results.Ok(new { answer });
});


app.Run();

public record QuestionRequest(string Question, string ConversationId);