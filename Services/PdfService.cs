
using UglyToad.PdfPig;
namespace RAGDemo.Services
{
    public class PdfService
    {
        public string ExtractText(string path)
        {
            var text = "";
            using (var document = PdfDocument.Open(path))
            {
                foreach (var page in document.GetPages())
                {
                    text += page.Text + "\n";
                }
            }

            return text;
        }
    }
}