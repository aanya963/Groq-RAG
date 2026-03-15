//This imports all classes inside:
using RAGDemo.Services;

class Program
{
    //static: belongs to the class itself, no obj needed
    //async: Allows await calls, Needed for API calls and database calls
    // Task: Async method return type: This program will run asynchronously.
    static async Task Main()
    {
        //creating objects of each service
        var pdfService = new PdfService();
        var chunkService = new ChunkService();
        var embeddingService = new EmbeddingService();
        var vectorDb = new VectorDbService();
        var groqService = new GroqService();


// - - - - - - - - - - First Phase — INDEXING THE PDF - - - - - - -  - - - - - 

        Console.WriteLine("Indexing PDF...");
        //STEP 1 = Extract Text from PDF
        /*
            documents.pdf
                ↓
            PdfService
                ↓
            Extract all text
                ↓
            Return string

            So text now contains entire PDF text.
        */


        var text = pdfService.ExtractText("Data/documents.pdf");

        //Step 2 = Split Into Chunks
        /*  Large documents are split into small pieces.
            //Example:
            Chunk1 → 500 chars
            Chunk2 → 500 chars
            Chunk3 → 500 chars
            Chunk4 → 500 chars

            Why?

            Because embeddings models have token limits.

            So we store small pieces.
        */
        var chunks = chunkService.ChunkText(text);
        
        /* Step 3 — Loop Through Chunks
            foreach (var chunk in chunks)

            Example:

            chunk1
            chunk2
            chunk3
            chunk4

            For each chunk we do:

            embedding → store in DB
        */
        foreach (var chunk in chunks)
        {
        /* Step 4 — Generate Embedding
            Embedding = vector representation of text
            "AI is powerful" →[0.234, -0.543, 0.998, ...]
        */
            var embedding = await embeddingService.GenerateEmbedding(chunk);

        /*Step 5 — Store in Vector Database
            Database table:documents
            content	: chunk text	
            embedding: vector

            So the DB stores: chunk text + vector
        */
            await vectorDb.InsertChunk(chunk, embedding);
        }
        
        //PDF → fully stored in vector database
        Console.WriteLine("PDF Indexed!");

        // ---- CHAT LOOP ----
        Console.WriteLine("\nRAG Chat Started");
        Console.WriteLine("Type 'exit' to quit");

        //Infinite Chat Loop UNTIL exit
        while (true)
        {
            Console.Write("\nAsk: ");
            var question = Console.ReadLine();

            if (question == null || question.ToLower() == "exit")
                break;

            //Convert Question to Embedding = vector representation of text
            var queryEmbedding = await embeddingService.GenerateEmbedding(question);
            /* Search Similar Chunks
                Vector DB searches: Which chunks are closest to question vector?

                SQL used internally: ORDER BY embedding <-> queryEmbedding

                This is vector similarity search.

                Result example:
                    Chunk about AI
                    Chunk about Machine Learning
                    Chunk about neural networks
            */
            var chunksFound = await vectorDb.SearchSimilar(queryEmbedding);

            //Combine Context : All chunks merged into one context string.
            var context = string.Join("\n", chunksFound);
            //Prompt sent to LLM: LLM reads the context and answers.
            var answer = await groqService.AskLLM(question, context);

            Console.WriteLine("\nAnswer:");
            Console.WriteLine(answer);
        }
    }
}



// WITHOUT USING POSTGRESQL

// using System.Text;
// using UglyToad.PdfPig;
// using RAGDemo.Services;

// class Program
// {
//     static async Task Main()
//     {
//         var groqApiKey = "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";

//         var embedder = new EmbeddingService();
//         var groq = new GroqService(groqApiKey);

//         Console.WriteLine("Reading PDF...");

//         var text = ExtractPdf("Data/documents.pdf");

//         var chunks = ChunkText(text);

//         Console.WriteLine($"Chunks created: {chunks.Count}");

//         var embeddings = new List<float[]>();

//         foreach (var chunk in chunks)
//         {
//             embeddings.Add(await embedder.GenerateEmbedding(chunk));
//         }

//         Console.WriteLine("Embeddings ready.");

//         while (true)
//         {
//             Console.Write("\nAsk (type 'exit' to quit): ");
//             var question = Console.ReadLine();

//             if (string.IsNullOrWhiteSpace(question))
//                 continue;

//             if (question.Trim().ToLower() == "exit")
//             {
//                 Console.WriteLine("Exiting chat. Goodbye!");
//                 break;
//             }

//             var queryEmbedding = await embedder.GenerateEmbedding(question);

//             var topChunks = GetTopChunks(queryEmbedding, chunks, embeddings);

//             var prompt = $"""
//                 You are a helpful assistant.

//                 First check the provided context.
//                 If the answer exists in the context, use it.

//                 If the answer is NOT in the context, answer using your general knowledge.
//                 And don't mention this "Unfortunately, the answer to your question is not present in the context. 
//                 However, as a helpful assistant, I can provide you with the current information available to me", in the response just answer.
//                 Context:
//                 {string.Join("\n---\n", topChunks)}

//                 Question: {question}
//                 """;

//             var answer = await groq.GenerateAnswer(prompt);

//             Console.WriteLine("\nAnswer:\n" + answer);
//         }
//     }

//     static string ExtractPdf(string path)
//     {
//         var text = new StringBuilder();

//         using var pdf = PdfDocument.Open(path);

//         foreach (var page in pdf.GetPages())
//             text.AppendLine(page.Text);

//         return text.ToString();
//     }

//     static List<string> ChunkText(string text, int size = 1000)
//     {
//         var chunks = new List<string>();

//         for (int i = 0; i < text.Length; i += size)
//         {
//             chunks.Add(text.Substring(i, Math.Min(size, text.Length - i)));
//         }

//         return chunks;
//     }

//     static float CosineSimilarity(float[] a, float[] b)
//     {
//         float dot = 0, magA = 0, magB = 0;

//         for (int i = 0; i < a.Length; i++)
//         {
//             dot += a[i] * b[i];
//             magA += a[i] * a[i];
//             magB += b[i] * b[i];
//         }

//         return dot / (float)(Math.Sqrt(magA) * Math.Sqrt(magB) + 1e-10);
//     }

//     static List<string> GetTopChunks(float[] query, List<string> chunks, List<float[]> embeddings)
//     {
//         return chunks
//             .Zip(embeddings, (c, e) => (chunk: c, score: CosineSimilarity(query, e)))
//             .OrderByDescending(x => x.score)
//             .Take(3)
//             .Select(x => x.chunk)
//             .ToList();
//     }
// }
