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

        public async Task<string> AskAsync(string question)
        {
            var queryEmbedding = await _embeddingService.GenerateEmbedding(question);
            var chunks = await _vectorDb.SearchSimilar(queryEmbedding);

            if (!chunks.Any())
                return "I couldn't find relevant information in the document to answer your question.";

            var context = string.Join("\n", chunks);
            return await _groqService.AskLLM(question, context);
        }
    }
}