using Amazon.Textract;
using Amazon.Textract.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;
using SixLabors.ImageSharp;

namespace AIServicesDemo.Pages
{
    public class QueryModel : PageModel
    {
        [BindProperty]
        public string Query { get; set; } = String.Empty;

        [BindProperty]
        public IFormFile? FormFile { get; set; }
        public string FileName { get; set; } = String.Empty;
        public string NewFileName { get; set; } = String.Empty;
        public string Result { get; set; } = String.Empty;

        private readonly IAmazonTextract _textractClient;
        private readonly IWebHostEnvironment _hostenvironment;

        public QueryModel(IAmazonTextract textractClient, IWebHostEnvironment hostenvironment)
        {
            _textractClient = textractClient;
            _hostenvironment = hostenvironment;
        }

        public void OnGet()
        {
        }

        public async Task OnPostAsync()
        {
            if (FormFile == null)
            {
                return;
            }
            // save image to display it
            var fileName = String.Format("{0}{1}", Guid.NewGuid().ToString(), Path.GetExtension(FormFile.FileName));
            var fullFileName = System.IO.Path.Combine(_hostenvironment.WebRootPath, "uploads", fileName);
            var newFileName = String.Format("{0}_id{1}", Guid.NewGuid().ToString(), System.IO.Path.GetExtension(FormFile.FileName));

            using (var stream = new FileStream(Path.Combine(_hostenvironment.WebRootPath, "uploads", fileName), FileMode.Create))
            {
                await FormFile.CopyToAsync(stream);
                FileName = fileName;
            }

            var memoryStream = new MemoryStream();
            await FormFile.CopyToAsync(memoryStream);

            var queryRequest = new AnalyzeDocumentRequest()
            {
                Document = new Document { Bytes = memoryStream },
                FeatureTypes = new List<string> { "QUERIES" },
                QueriesConfig = new QueriesConfig
                {
                    Queries = new List<Query> { new Query { Text = Query } }
                }
            };

            var queryResponse = await _textractClient.AnalyzeDocumentAsync(queryRequest);

            var stringBuilder = new StringBuilder();

            stringBuilder.AppendFormat("Query: <b>{0}</b><br>", Query);
            foreach (var block in queryResponse.Blocks)
            {
                if (block.BlockType.Value == "QUERY_RESULT")
                {
                    stringBuilder.AppendFormat(
                                "Answer: <b>{0}</b>, Confidence: <b>{1}</b><br>",
                                block.Text,
                                block.Confidence);

                    // Load image to modify with face bounding box rectangle
                    using (var image = SixLabors.ImageSharp.Image.Load(fullFileName))
                    {
                        // Get the bounding box
                        var boundingBox = block.Geometry.BoundingBox;

                        // Draw the rectangle using the bounding box values
                        image.DrawRectangleUsingBoundingBox(boundingBox);

                        // Save the new image
                        image.SaveAsJpeg(System.IO.Path.Combine(_hostenvironment.WebRootPath, "uploads", newFileName));
                        NewFileName = newFileName;
                    }
                }
            }

            Result = stringBuilder.ToString();
        }
    }
}
