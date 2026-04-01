namespace RAGDemo.Services
{
    public class RagService
    {
        private readonly PdfService _pdfService;
        private readonly ChunkService _chunkService;
        private readonly EmbeddingService _embeddingService;
        private readonly VectorDbService _vectorDb;
        private readonly GroqService _groqService;
        private readonly FileHashService _hashService;
        private readonly RedisService _redisService;

        public RagService(
            PdfService pdfService,
            ChunkService chunkService,
            EmbeddingService embeddingService,
            VectorDbService vectorDb,
            GroqService groqService,
            FileHashService hashService,
            RedisService redisService)
        {
            _pdfService = pdfService;
            _chunkService = chunkService;
            _embeddingService = embeddingService;
            _vectorDb = vectorDb;
            _groqService = groqService;
            _hashService = hashService;
            _redisService = redisService;
        }

        // -----------------------------
        // INDEXING PHASE
        // -----------------------------
        public async Task IndexDocumentAsync(string pdfPath)
        {
            var fileHash = _hashService.ComputeHash(pdfPath);

            // Avoid duplicate indexing
            if (await _vectorDb.IsAlreadyIndexed(fileHash))
            {
                Console.WriteLine("Document already indexed. Skipping...");
                return;
            }

            Console.WriteLine("Indexing PDF...");
            var text = _pdfService.ExtractText(pdfPath);
            var chunks = _chunkService.ChunkText(text);

            foreach (var chunk in chunks)
            {
                var embedding = await _embeddingService.GenerateEmbedding(chunk);
                await _vectorDb.InsertChunk(chunk, embedding);
            }

            await _vectorDb.MarkAsIndexed(fileHash, pdfPath);
            Console.WriteLine("PDF Indexed!");
        }

        // -----------------------------
        // QUERY PHASE (RAG + MEMORY)
        // -----------------------------
        public async Task<string> AskAsync(string question, string conversationId)
        {
            if (string.IsNullOrEmpty(conversationId))
                conversationId = "default";

            // 1. Get history from Redis
            var history = await _redisService.GetConversation(conversationId);

            // 2. Add current user question FIRST
            history.Add(new ChatMessage("user", question));

            // 3. Retrieve relevant chunks
            var queryEmbedding = await _embeddingService.GenerateEmbedding(question);
            var chunks = await _vectorDb.SearchSimilar(queryEmbedding);

            if (!chunks.Any())
            {
                var notFound = "I couldn't find relevant information in the document to answer your question.";

                history.Add(new ChatMessage("assistant", notFound));

                await _redisService.SaveConversation(conversationId, history);

                return notFound;
            }

            // 4. Build context
            var context = string.Join("\n", chunks);

            // 5. Call LLM
            var answer = await _groqService.AskLLM(question, context, history);

            // 6. Save assistant response
            history.Add(new ChatMessage("assistant", answer));

            // 7. Trim history (avoid token overflow)
            if (history.Count > 10)
                history.RemoveRange(0, history.Count - 10);

            // 8. Save back to Redis
            await _redisService.SaveConversation(conversationId, history);

            return answer;
        }
    }
}