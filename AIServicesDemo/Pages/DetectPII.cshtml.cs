using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;
using Amazon.Comprehend;
using Amazon.Comprehend.Model;
using Amazon.Textract;
using Amazon.Textract.Model;

namespace AIServicesDemo.Pages
{
    public class DetectPIIModel : PageModel
    {
        [BindProperty]
        public IFormFile? FormFile { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;
        
        private readonly IAmazonTextract _textractClient;
        private readonly IAmazonComprehend _comprehendClient;
        private readonly IWebHostEnvironment _hostEnvironment;
        
        public DetectPIIModel(IAmazonTextract textractClient, IAmazonComprehend comprehendClient, IWebHostEnvironment hostEnvironment)
        {
            _textractClient = textractClient;
            _comprehendClient = comprehendClient;
            _hostEnvironment = hostEnvironment;
        }

        public void OnGet()
        {
        }
        
        
        public async Task OnPostDetectPIIAsync()
        {
            if (FormFile == null)
            {
                return;
            }
            // save document image to display it
            FileName = $"{Guid.NewGuid().ToString()}{System.IO.Path.GetExtension(FormFile.FileName)}";
            var fullFileName = System.IO.Path.Combine(_hostEnvironment.WebRootPath, "uploads", FileName);

            await using (var stream = new FileStream(fullFileName, FileMode.Create))
            {
                await FormFile.CopyToAsync(stream);
            }

            var memoryStream = new MemoryStream();
            await FormFile.CopyToAsync(memoryStream);

            var detectDocumentTextRequest = new DetectDocumentTextRequest()
            {
                Document = new Document { Bytes = memoryStream }
            };

            var detectDocumentTextResponse = await _textractClient.DetectDocumentTextAsync(detectDocumentTextRequest);

            var stringBuilder = new StringBuilder();


            foreach (var block in detectDocumentTextResponse.Blocks)
            {
                if (block.BlockType.Value == "LINE")
                {
                    stringBuilder.AppendFormat("{0}\n", block.Text);
                }
            }

            var text = stringBuilder.ToString();
            var request = new DetectPiiEntitiesRequest()
            {
                Text = text,
                LanguageCode = "en"
            };

            var response = await _comprehendClient.DetectPiiEntitiesAsync(request);

            stringBuilder.AppendLine("<br><br>PII:<br>");
            stringBuilder.AppendLine("==========================<br>");

            foreach (var entity in response.Entities)
            {
                stringBuilder.AppendFormat(
                    "Text: <b>{0}</b>, Type: <b>{1}</b>, Score: <b>{2}</b><br>",
                    text.Substring(entity.BeginOffset, entity.EndOffset - entity.BeginOffset),
                    entity.Type,
                    entity.Score);
            }
            
            Result = stringBuilder.ToString();
        }
       
    }
}
