using DotNetEnv;
using System.Text;
using System.Text.Json;

namespace RAGDemo.Services
{
    public class GroqService
    {
       private readonly HttpClient _http;
        private readonly string apiKey;

        static GroqService()
        {
            // Load .env file once
            Env.Load();
        }

        public GroqService()
        {
            _http = new HttpClient();
            apiKey = Env.GetString("GROQ_API_KEY");
            _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        }

        public async Task<string> AskLLM(string question, string context)
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
            ";

            var userPrompt = $@"
                Document Context:
                ----------------
                {context}
                ----------------

                Question: {question}

                Answer based strictly on the context above:
            ";

            var body = new
            {
                model = "llama-3.1-8b-instant",
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user",   content = userPrompt   }
                }
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