using System.Text;
using System.Text.Json;

namespace RAGDemo.Services
{
    public class EmbeddingService
    {
        private readonly HttpClient _http;

        // HttpClient is now injected, not created internally, HttpClient is injected from outside
        public EmbeddingService(HttpClient http)
        {
            _http = http; // swappable, testable
        }

        public async Task<float[]> GenerateEmbedding(string text)
        {
            var body = new
            {
                model = "nomic-embed-text",
                prompt = text
            };

            var content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _http.PostAsync(
                "http://localhost:11434/api/embeddings",
                content
            );

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            return doc.RootElement
                .GetProperty("embedding")
                .EnumerateArray()
                .Select(x => x.GetSingle())
                .ToArray();
        }
    }
}
