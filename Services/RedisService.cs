using StackExchange.Redis;
using System.Text.Json;

namespace RAGDemo.Services
{
    public class RedisService
    {
        private readonly IDatabase _db;

        public RedisService()
        {
            var redis = ConnectionMultiplexer.Connect("localhost:6379");
            _db = redis.GetDatabase();
        }

        // Save conversation
        public async Task SaveConversation(string conversationId, List<ChatMessage> messages)
        {
            var json = JsonSerializer.Serialize(messages);
            await _db.StringSetAsync(conversationId, json);
        }

        // Get conversation
        public async Task<List<ChatMessage>> GetConversation(string conversationId)
        {
            var value = await _db.StringGetAsync(conversationId);

            if (value.IsNullOrEmpty)
                return new List<ChatMessage>();

            // return JsonSerializer.Deserialize<List<ChatMessage>>(value!) ?? new List<ChatMessage>();
            return JsonSerializer.Deserialize<List<ChatMessage>>(value.ToString()) ?? new List<ChatMessage>();
        }
    }
}