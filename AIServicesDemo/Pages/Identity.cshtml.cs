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
        public IFormFile? IdentityFormFile { get; set; }
        [BindProperty]
        public IFormFile? PhotoFormFile { get; set; }
        public string IdentityFileName { get; set; } = String.Empty;
        public string PhotoFileName { get; set; } = String.Empty;
        public string NewPhotoFileName { get; set; } = String.Empty;
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
            if ((IdentityFormFile == null) || (PhotoFormFile == null))
            {
                return;
            }
            // save id to display it
            var identityFileName = String.Format("{0}{1}", Guid.NewGuid().ToString(), System.IO.Path.GetExtension(IdentityFormFile.FileName));
            var fullIdentityFileName = System.IO.Path.Combine(_hostenvironment.WebRootPath, "uploads", identityFileName);

            using (var stream = new FileStream(fullIdentityFileName, FileMode.Create))
            {
                await IdentityFormFile.CopyToAsync(stream);
                IdentityFileName = identityFileName;
            }

            // save photo to display it
            var photoFileName = String.Format("{0}{1}", Guid.NewGuid().ToString(), System.IO.Path.GetExtension(PhotoFormFile.FileName));
            var fullPhotoFileName = System.IO.Path.Combine(_hostenvironment.WebRootPath, "uploads", photoFileName);
            var newPhotoFileName = String.Format("{0}_id{1}", Guid.NewGuid().ToString(), System.IO.Path.GetExtension(IdentityFormFile.FileName));

            using (var stream = new FileStream(fullPhotoFileName, FileMode.Create))
            {
                await PhotoFormFile.CopyToAsync(stream);
                PhotoFileName = photoFileName;
            }

            var identityMemoryStream = new MemoryStream();
            await IdentityFormFile.CopyToAsync(identityMemoryStream);

            var photoMemoryStream = new MemoryStream();
            await PhotoFormFile.CopyToAsync(photoMemoryStream);

            var compareFacesRequest = new CompareFacesRequest()
            {
                SourceImage = new Amazon.Rekognition.Model.Image { Bytes = identityMemoryStream },
                TargetImage = new Amazon.Rekognition.Model.Image { Bytes = photoMemoryStream }
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
                using (var image = SixLabors.ImageSharp.Image.Load(fullPhotoFileName))
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
                    image.SaveAsJpeg(System.IO.Path.Combine(_hostenvironment.WebRootPath, "uploads", newPhotoFileName));
                    NewPhotoFileName = newPhotoFileName;
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
