/*
This imports the PdfPig library.

What is PdfPig?
A .NET library for reading PDF files.
*/

using UglyToad.PdfPig;
/*
A namespace is just a logical grouping of classes.
This class belongs to the Services module.
*/
namespace RAGDemo.Services
{
    /*
        This follows a design principle called:
        Single Responsibility Principle

        Each class should have one job only.
    */
    public class PdfService
    {
        public string ExtractText(string path)
        {
            var text = "";
            /* 
                1. Opens the PDF : PdfDocument.Open(path)
                    Example:
                    PdfDocument.Open("Data/documents.pdf")
                2. loads the PDF into memory.
                    document contains:
                        PDF structure
                        pages
                        text objects
                        metadata
                3. using(...)
                    It ensures the resource is automatically closed.
                    If we don’t close them → memory leaks.
                    So when this block ends:
                        document.Dispose()
            */
            using (var document = PdfDocument.Open(path))
            {
                //GetPages() returns an enumerable collection of pages.
                foreach (var page in document.GetPages())
                {
                    //Extract Text from Each Page
                    text += page.Text + "\n";
                }
            }

            return text;
        }
    }
}