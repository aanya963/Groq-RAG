using System.Text;

namespace RAGDemo.Services
{
    public class ChunkService
    {
        public List<string> ChunkText(string text, int chunkSize = 500, int overlap=100)
        {
            var chunks = new List<string>();
            // Split on sentence boundaries instead of raw character count
            var sentences = text.Split(new [] { ". ", ".\n", "! ", "? " }, StringSplitOptions.RemoveEmptyEntries);
            
            var currentChunk = new StringBuilder();
            

            foreach (var sentence in sentences)
            {
                // If adding this sentence exceeds chunkSize, save current chunk and start new one

                if(currentChunk.Length + sentence.Length > chunkSize && currentChunk.Length > 0)
                {
                    chunks.Add(currentChunk.ToString().Trim());
                    
                    // Carry the last `overlap` characters into the next chunk
                    var currentText = currentChunk.ToString();
                    var overlapText = currentText.Length > overlap
                        ? currentText.Substring(currentText.Length - overlap)
                        : currentText;
                    
                    currentChunk.Clear();
                    currentChunk.Append(overlapText);

                }
                currentChunk.Append(sentence + ". ");
            }
            // Don't forget the last chunk
            if (currentChunk.Length > 0)
                chunks.Add(currentChunk.ToString().Trim());

            return chunks;
        }
    }
}