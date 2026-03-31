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

        // In-memory store: conversationId → list of messages
        private readonly Dictionary<string, List<ChatMessage>> _conversations = new();

        public RagService(
            PdfService pdfService,
            ChunkService chunkService,
            EmbeddingService embeddingService,
            VectorDbService vectorDb,
            GroqService groqService,
            FileHashService hashService)
        {
            _pdfService = pdfService;
            _chunkService = chunkService;
            _embeddingService = embeddingService;
            _vectorDb = vectorDb;
            _groqService = groqService;
            _hashService = hashService;
        }

        public async Task IndexDocumentAsync(string pdfPath)
        {
            var fileHash = _hashService.ComputeHash(pdfPath);

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

        public async Task<string> AskAsync(string question, string conversationId)
        {
            // If client sends no conversationId, generate a fallback
            if (string.IsNullOrEmpty(conversationId))
                conversationId = "default";
            // Get or create history for this conversation
            if (!_conversations.ContainsKey(conversationId))
                _conversations[conversationId] = new List<ChatMessage>();

            var history = _conversations[conversationId];

            // Retrieve relevant chunks
            var queryEmbedding = await _embeddingService.GenerateEmbedding(question);
            var chunks = await _vectorDb.SearchSimilar(queryEmbedding);

            if (!chunks.Any())
            {
                var notFound = "I couldn't find relevant information in the document to answer your question.";
                
                // Still save to history so follow-ups work
                history.Add(new ChatMessage("user", question));
                history.Add(new ChatMessage("assistant", notFound));
                return notFound;
            }

            var context = string.Join("\n", chunks);
            var answer = await _groqService.AskLLM(question, context, history);

            // Save this turn to history
            history.Add(new ChatMessage("user", question));
            history.Add(new ChatMessage("assistant", answer));

            // Keep last 10 messages to avoid token limit issues
            if (history.Count > 10)
                history.RemoveRange(0, history.Count - 10);

            return answer;
        }

        internal async Task AskAsync(string question, object conversationId)
        {
            throw new NotImplementedException();
        }
    }
}