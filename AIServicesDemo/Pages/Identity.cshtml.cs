using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;

namespace AIServicesDemo.Pages
{
    public class IdentityCheckModel : PageModel
    {
        [BindProperty]
        public IFormFile? FormFile1 { get; set; }
        [BindProperty]
        public IFormFile? FormFile2 { get; set; }
        public string FileName1 { get; set; } = String.Empty;
        public string FileName2 { get; set; } = String.Empty;
        public string NewFileName1 { get; set; } = String.Empty;
        public string Result { get; set; } = String.Empty;

        private readonly IAmazonRekognition _rekognitionClient;
        private readonly IWebHostEnvironment _hostenvironment;

        public IdentityCheckModel(IAmazonRekognition rekognitionClient, IWebHostEnvironment hostenvironment)
        {
            _rekognitionClient = rekognitionClient;
            _hostenvironment = hostenvironment;
        }

        public void OnGet()
        {
        }

        public async Task OnPostIdentitiesAsync()
        {
            if ((FormFile1 == null) || (FormFile2 == null))
            {
                return;
            }
            // save image to display it
            var fileName1 = String.Format("{0}{1}", Guid.NewGuid().ToString(), System.IO.Path.GetExtension(FormFile1.FileName));
            var fullFileName1 = System.IO.Path.Combine(_hostenvironment.WebRootPath, "uploads", fileName1);
            var newFileName1 = String.Format("{0}_id{1}", Guid.NewGuid().ToString(), System.IO.Path.GetExtension(FormFile1.FileName));

            using (var stream = new FileStream(fullFileName1, FileMode.Create))
            {
                await FormFile1.CopyToAsync(stream);
                FileName1 = fileName1;
            }

            // save image to display it
            var fileName2 = String.Format("{0}{1}", Guid.NewGuid().ToString(), System.IO.Path.GetExtension(FormFile2.FileName));
            var fullFileName2 = System.IO.Path.Combine(_hostenvironment.WebRootPath, "uploads", fileName2);

            using (var stream = new FileStream(fullFileName2, FileMode.Create))
            {
                await FormFile2.CopyToAsync(stream);
                FileName2 = fileName2;
            }

            var memoryStream1 = new MemoryStream();
            await FormFile1.CopyToAsync(memoryStream1);

            var memoryStream2 = new MemoryStream();
            await FormFile2.CopyToAsync(memoryStream2);

            var compareFacesRequest = new CompareFacesRequest()
            {
                SourceImage = new Amazon.Rekognition.Model.Image { Bytes = memoryStream1 },
                TargetImage = new Amazon.Rekognition.Model.Image { Bytes = memoryStream2 }
            };

            var compareFacesResponse = await _rekognitionClient.CompareFacesAsync(compareFacesRequest);

            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("Text:<br>");
            stringBuilder.AppendLine("==========================<br>");

            if (compareFacesResponse.FaceMatches.Count > 0)
            {
                stringBuilder.AppendFormat(
                    "Face similarity: <b>{0}</b><br>",
                    compareFacesResponse.FaceMatches[0].Similarity);

                // Load image to modify with face bounding box rectangle
                using (var image = SixLabors.ImageSharp.Image.Load(fullFileName2))
                {
                    var faceDetail = compareFacesResponse.FaceMatches[0].Face;

                    // Get the bounding box
                    var boundingBox = faceDetail.BoundingBox;

                    // Draw the rectangle using the bounding box values
                    // They are percentages so scale them to picture
                    image.Mutate(x => x.DrawLines(
                        Rgba32.ParseHex("FF0000"),
                        5,
                        new PointF[]
                        {
                            new PointF(image.Width * boundingBox.Left, image.Height * boundingBox.Top),
                            new PointF(image.Width * (boundingBox.Left + boundingBox.Width),
                                image.Height * boundingBox.Top),
                            new PointF(image.Width * (boundingBox.Left + boundingBox.Width),
                                image.Height * (boundingBox.Top + boundingBox.Height)),
                            new PointF(image.Width * boundingBox.Left,
                                image.Height * (boundingBox.Top + boundingBox.Height)),
                            new PointF(image.Width * boundingBox.Left, image.Height * boundingBox.Top),
                        }
                    ));

                    // Save the new image
                    image.SaveAsJpeg(System.IO.Path.Combine(_hostenvironment.WebRootPath, "uploads", newFileName1));
                    NewFileName1 = newFileName1;
                }

            }
            else
            {
                stringBuilder.AppendLine("No matching faces");
            }

            Result = stringBuilder.ToString();

        }
    }
}
