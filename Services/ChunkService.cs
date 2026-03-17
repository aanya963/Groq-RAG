/*
A namespace is just a logical grouping of classes.
This class belongs to the Services module.
*/
namespace RAGDemo.Services
{
    public class ChunkService
    {
        /* 
            This defines a service class responsible for : Text → smaller pieces

            Why do we need this?
                Because LLMs and embedding models cannot process huge documents at once.
            Example:
                A PDF might contain:
                15,000 characters
            But embedding models work best with small chunks.

            Function returns : List of text chunks
        */
        public List<string> ChunkText(string text, int chunkSize = 500)
        {
            //var means the compiler will automatically determine the variable type.
            //List<string> -> A dynamic collection that stores strings -> a resizable array.
            var chunks = new List<string>();

            for (int i = 0; i < text.Length; i += chunkSize)
            {
                var length = Math.Min(chunkSize, text.Length - i);
                chunks.Add(text.Substring(i, length));
            }

            return chunks;
        }
    }
}