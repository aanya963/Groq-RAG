/* 
System.Text: 
Provides encoding utilities like: UTF8, ASCII
Used when sending HTTP request content.
Convert the text into UTF-8 bytes before sending it over the network.

System.Text.Json
Serialize objects → JSON
Parse JSON → objects

This Service is used in two places in program:
1️⃣ When indexing PDF chunks
2️⃣ When embedding user questions
*/
using System.Text;
using System.Text.Json;

namespace RAGDemo.Services
{
    public class EmbeddingService
    {
        /*
            _http is a variable that stores an HTTP client object.
            So _http is the object that sends HTTP requests.
            here we send API requests to Ollama.
            Readonly : Value can only be assigned once.
        */
        private readonly HttpClient _http;
        /* The constructor runs when the object is created.
            var embeddingService = new EmbeddingService();
            So now:
                embeddingService._http
        */
        public EmbeddingService()
        {
            //creates a new HttpClient object and assigns it to that variable.
            _http = new HttpClient();
        }
        /*
            public : Accessible from outside.
            async : Allows await for HTTP calls.
            Return type : array of floats.(Task<float[]>)
        */
        public async Task<float[]> GenerateEmbedding(string text)
        {
            //Creating Request Body
            var body = new
            {
                model = "nomic-embed-text",
                prompt = text
            };
            /* Convert Body to JSON
                JsonSerializer.Serialize(body) : Turns the object into JSON.
                Encoding.UTF8
                    Specifies character encoding.
                    Most APIs require UTF-8 encoding.
                    Convert the text into UTF-8 bytes before sending it over the network.
                Content Type : application/json
                    This tells the server:
                    "I'm sending JSON data"
            */
            var content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json"
            );
            //This sends an HTTP request.
            var response = await _http.PostAsync(
                "http://localhost:11434/api/embeddings",
                content
            );
            //Read the Response
            var json = await response.Content.ReadAsStringAsync();
            //This converts the JSON string into a structured document object.
            //doc represents the entire JSON response.
            using var doc = JsonDocument.Parse(json);
            /* Extract the Embedding Vector
                retrieves the embedding field from JSON.
                EnumerateArray() : This converts the JSON array into something we can loop through.
                Select(x => x.GetSingle()) : Converts each JSON number to float.
                ToArray() : Creates float array
            */
            var vector = doc.RootElement
                .GetProperty("embedding")
                .EnumerateArray()
                .Select(x => x.GetSingle())
                .ToArray();

            return vector;
        }
    }
}