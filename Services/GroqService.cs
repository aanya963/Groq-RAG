using DotNetEnv;
using System.Text;
using System.Text.Json;

namespace RAGDemo.Services
{
    public record ChatMessage(string Role, string Content);

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
                You are a precise document assistant. Your job is to answer questions
                strictly based on the provided document context.

                Rules you must follow:
                - ONLY use information from the provided context to answer
                - If the context does not contain enough information, respond exactly with:
                  'I don't have enough information in the document to answer this question.'
                - Never use outside knowledge or make assumptions beyond what is in the context
                - Keep answers concise and factual
                - Do not mention the word 'context' in your response
                - Do not make up information under any circumstances
                - You have access to the conversation history — use it to understand follow-up questions
            ";

            // Build messages array: system + history + new user message with context
            var messages = new List<object>
            {
                new { role = "system", content = systemPrompt }
            };

            // Add previous conversation turns
            foreach (var msg in history)
            {
                messages.Add(new { role = msg.Role, content = msg.Content });
            }

            // Add the new question with retrieved context attached
            messages.Add(new
            {
                role = "user",
                content = $@"
                    Document Context:
                    ----------------
                    {context}
                    ----------------

                    Question: {question}

                    Answer based strictly on the context above:"
            });

            var body = new
            {
                model = "llama-3.1-8b-instant",
                messages
            };

            var content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _http.PostAsync(
                "https://api.groq.com/openai/v1/chat/completions",
                content
            );

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            return doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();
        }
    }
}