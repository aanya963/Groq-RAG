using DotNetEnv;
using System.Text;
using System.Text.Json;

namespace RAGDemo.Services
{
    public record ChatMessage(string Role, string Content);
    public record ToolCall(string Id, string FunctionName, string ArgumentsJson);

    public class GroqService
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;
        static GroqService() { Env.Load(); }
        public GroqService(HttpClient http)
        {
            _http = http;
            _apiKey = Env.GetString("GROQ_API_KEY");
            _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        }
     public async Task<string> AskLLM(string question, string context, List<ChatMessage> history)
{
    var systemPrompt = @"
        You are a precise document assistant.

        Your job is to answer questions strictly using the provided document content.

        Rules:
        - Use ONLY the provided document content to answer
        - If the answer is not present, respond exactly with:
          'I don't have enough information in the document to answer this question.'
        - Do NOT use outside knowledge
        - Keep answers clear, concise, and factual
        - Do NOT mention 'context' in your answer
        - Use conversation history to understand follow-up questions
    ";

    var messages = new List<object>
    {
        new { role = "system", content = systemPrompt }
    };

    // Add conversation history
    foreach (var msg in history)
    {
        messages.Add(new
        {
            role = msg.Role,
            content = msg.Content
        });
    }

    // Add context + question (VERY IMPORTANT: clean format)
    messages.Add(new
    {
        role = "user",
        content = $@"
Document content:
{context}

Question:
{question}

Answer:"
    });

    var body = new
    {
        model = "llama-3.1-8b-instant",
        messages = messages
    };

    var requestContent = new StringContent(
        JsonSerializer.Serialize(body),
        Encoding.UTF8,
        "application/json"
    );

    var response = await _http.PostAsync(
        "https://api.groq.com/openai/v1/chat/completions",
        requestContent
    );

    var json = await response.Content.ReadAsStringAsync();
    using var doc = JsonDocument.Parse(json);

    return doc.RootElement
        .GetProperty("choices")[0]
        .GetProperty("message")
        .GetProperty("content")
        .GetString() ?? "";

    }
  }
}