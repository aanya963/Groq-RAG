/* 
System.Text: 
Provides encoding utilities like: UTF8, ASCII
Used when sending HTTP request content.

System.Text.Json
Serialize objects → JSON
Parse JSON → objects

DotNetEnv : Loads environment variables from .env.
*/
using DotNetEnv;
using System.Text;
using System.Text.Json;

namespace RAGDemo.Services
{
    public class GroqService
    {
        //HttpClient is used to make HTTP requests.
        //apiKey Stores the Groq API key.
       private readonly HttpClient _http;
        private readonly string apiKey;

        // Static constructor runs once for the class, not every time you create an object.
        static GroqService()
        {
            // Load .env file once
            Env.Load();
        }

        public GroqService()
        {
            //Creates the HTTP client.
            _http = new HttpClient();
            // Get API key from .env
            apiKey = Env.GetString("GROQ_API_KEY");
            /*Add authorization header
            This tells the API:
                I am authenticated
                The request header becomes:
                Authorization: Bearer sk_123abc
                Without this → API call will fail.
            */
            _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        }

        public async Task<string> AskLLM(string question, string context)
        {
            var prompt = $@"
                You are a helpful assistant.

                Use the provided context when relevant.
                If the context does not contain the answer, use your general knowledge. 
                Don't mention this 'The context provided does not contain information about the Prime Minister of India. ' in your response.
                Just give the answer.

                Context:
                {context}

                Question:
                {question}
            ";


            var body = new
            {
                model = "llama-3.1-8b-instant",
                messages = new[]
                {
                    new { role = "user", content = prompt }
                }
            };
            //Convert to JSON
            var content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json"
            );
            //sends the request.

            var response = await _http.PostAsync(
                "https://api.groq.com/openai/v1/chat/completions",
                content
            );
            //Read the response
            var json = await response.Content.ReadAsStringAsync();
            //This converts the JSON string into a structure C# can navigate.
            using var doc = JsonDocument.Parse(json);
            //Extract the answer
            return doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();
        }
    }
}