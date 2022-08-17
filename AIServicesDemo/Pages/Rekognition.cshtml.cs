using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Amazon.Textract;
using Amazon.Textract.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;

namespace AIServicesDemo.Pages
{
    public class RekognitionModel : PageModel
    {
        [BindProperty]
        public IFormFile FormFile { get; set; }
        public string FileName { get; set; }
        public string NewFileName { get; set; }
        public string Result { get; set; }

        private readonly IAmazonTextract _textractClient;
        private readonly IAmazonRekognition _rekognitionClient;
        private readonly IWebHostEnvironment _hostenvironment;

        public RekognitionModel(IAmazonTextract textractClient, IAmazonRekognition rekognitionClient, IWebHostEnvironment hostenvironment)
        {
            _textractClient = textractClient;
            _rekognitionClient = rekognitionClient;
            _hostenvironment = hostenvironment;
        }

        public void OnGet()
        {
        }

        public async Task OnPostFacesAsync()
        {
            // save image to display it
            var fileName = String.Format("{0}.{1}", Guid.NewGuid().ToString(), Path.GetExtension(FormFile.FileName));
            var fullFileName = Path.Combine(_hostenvironment.WebRootPath, "uploads", fileName);
            var newFileName = String.Format("{0}_faces.{1}", Guid.NewGuid().ToString(), Path.GetExtension(FormFile.FileName));

            using (var stream = new FileStream(fullFileName, FileMode.Create))
            {
                await FormFile.CopyToAsync(stream);
                FileName = fileName;
            }

            var memoryStream = new MemoryStream();
            await FormFile.CopyToAsync(memoryStream);

            var detectFacesRequest = new DetectFacesRequest()
            {
                Image = new Amazon.Rekognition.Model.Image { Bytes = memoryStream }
            };

            var detectFacesResponse = await _rekognitionClient.DetectFacesAsync(detectFacesRequest);

            if (detectFacesResponse.FaceDetails.Count > 0)
            {
                // Load a bitmap to modify with face bounding box rectangles
                var facesHighlighted = new Bitmap(fullFileName);
                var pen = new Pen(Color.Red, 3);

                // Create a graphics context
                using (var graphics = Graphics.FromImage(facesHighlighted))
                {
                    foreach (var faceDetail in detectFacesResponse.FaceDetails)
                    {
                        // Get the bounding box
                        var boundingBox = faceDetail.BoundingBox;

                        // Draw the rectangle using the bounding box values
                        // They are percentages so scale them to picture
                        graphics.DrawRectangle(pen, x: facesHighlighted.Width * boundingBox.Left,
                            y: facesHighlighted.Height * boundingBox.Top,
                            width: facesHighlighted.Width * boundingBox.Width,
                            height: facesHighlighted.Height * boundingBox.Height);
                    }

                    // Save the new image
                    facesHighlighted.Save(Path.Combine(_hostenvironment.WebRootPath, "uploads", newFileName), ImageFormat.Jpeg);
                    NewFileName = newFileName;
                }
            }
        }

        public async Task OnPostEntitiesAsync()
        {
            // save image to display it
            var fileName = String.Format("{0}.{1}", Guid.NewGuid().ToString(), Path.GetExtension(FormFile.FileName));
            var fullFileName = Path.Combine(_hostenvironment.WebRootPath, "uploads", fileName);

            using (var stream = new FileStream(fullFileName, FileMode.Create))
            {
                await FormFile.CopyToAsync(stream);
                FileName = fileName;
            }

            var memoryStream = new MemoryStream();
            await FormFile.CopyToAsync(memoryStream);

            var detectLabelsRequest = new DetectLabelsRequest()
            {
                Image = new Amazon.Rekognition.Model.Image { Bytes = memoryStream }
            };

            var detectLabelsResponse = await _rekognitionClient.DetectLabelsAsync(detectLabelsRequest);


            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("Labels:<br>");
            stringBuilder.AppendLine("==========================<br>");

            foreach (var label in detectLabelsResponse.Labels)
            {
                stringBuilder.AppendFormat(
                    "Label: <b>{0}</b>, Confidence: <b>{1}</b><br>",
                    label.Name,
                    label.Confidence);
            }

            Result = stringBuilder.ToString();

        }

        public async Task OnPostPPEAsync()
        {
            // save image to display it
            var fileName = String.Format("{0}.{1}", Guid.NewGuid().ToString(), Path.GetExtension(FormFile.FileName));
            var fullFileName = Path.Combine(_hostenvironment.WebRootPath, "uploads", fileName);

            using (var stream = new FileStream(fullFileName, FileMode.Create))
            {
                await FormFile.CopyToAsync(stream);
                FileName = fileName;
            }

            var memoryStream = new MemoryStream();
            await FormFile.CopyToAsync(memoryStream);

            var detectPPERequest = new DetectProtectiveEquipmentRequest()
            {
                Image = new Amazon.Rekognition.Model.Image { Bytes = memoryStream }
            };

            var detectPPEResponse = await _rekognitionClient.DetectProtectiveEquipmentAsync(detectPPERequest);


            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("PPE:<br>");
            stringBuilder.AppendLine("==========================<br>");

            foreach (var person in detectPPEResponse.Persons)
            {
                foreach (var bodyPart in person.BodyParts)
                {
                    foreach (var eq in bodyPart.EquipmentDetections)
                    {
                        stringBuilder.AppendFormat(
                    "Body part: <b>{0}</b>, Type: <b>{1}</b>, Covered: <b>{2}</b><br>",
                    bodyPart.Name.Value,
                    eq.Type.Value,
                    eq.CoversBodyPart.Value);

                    }

                }

                Result = stringBuilder.ToString();

            }
        }
    }
}
