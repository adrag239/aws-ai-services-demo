using Amazon.Textract;
using Amazon.Textract.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;
using System.Text.Encodings.Web;

namespace AIServicesDemo.Pages
{
    public class AnalyzeIDModel : PageModel
    {
        [BindProperty]
        public IFormFile? FormFile { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;

        private readonly IAmazonTextract _textractClient;
        private readonly IWebHostEnvironment _hostEnvironment;

        public AnalyzeIDModel(IAmazonTextract textractClient, IWebHostEnvironment hostEnvironment)
        {
            _textractClient = textractClient;
            _hostEnvironment = hostEnvironment;
        }

        public void OnGet()
        {
        }

        public async Task OnPostAsync()
        {
            if (FormFile == null)
                return;
            
            // save image to display it
            FileName = $"{Guid.NewGuid().ToString()}{System.IO.Path.GetExtension(FormFile.FileName)}";
            var fullFileName = System.IO.Path.Combine(_hostEnvironment.WebRootPath, "uploads", FileName);

            await using (var stream = new FileStream(fullFileName, FileMode.Create))
            {
                await FormFile.CopyToAsync(stream);
            }

            var memoryStream = new MemoryStream();
            await FormFile.CopyToAsync(memoryStream);

            var request = new AnalyzeIDRequest()
            {
                DocumentPages = new List<Document> { new Document { Bytes = memoryStream }}
            };

            var response = await _textractClient.AnalyzeIDAsync(request);

            var stringBuilder = new StringBuilder();

            foreach (var document in response.IdentityDocuments)
            {
                stringBuilder.AppendLine("==========================<br>");
                foreach (var field in document.IdentityDocumentFields)
                {
                    stringBuilder.AppendFormat(
                        "Type: <b>{0}</b>, Text: <b>{1}</b><br>",
                        field.Type.Text,
                        field.ValueDetection.Text);   
                }
            }

            Result = stringBuilder.ToString();
        }
    }
}
