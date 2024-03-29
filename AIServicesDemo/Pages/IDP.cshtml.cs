using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;
using Amazon.Comprehend;
using Amazon.Comprehend.Model;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using Amazon.Textract;
using Amazon.Textract.Model;

namespace AIServicesDemo.Pages
{
    public class IDPModel : PageModel
    {
        [BindProperty]
        public IFormFile? FormFile { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;

        private readonly IAmazonComprehend _comprehendClient;
        private readonly IAmazonTextract _textractClient;
        private readonly IWebHostEnvironment _hostEnvironment;

        private const string BUCKET_NAME = "adrag-idp";
        //private const string DOCUMENT_CLASSIFIER_NAME = "ReInvent-IDP-Demo-Doc-Classifier";
        private const string DOCUMENT_CLASSIFIER_NAME = "ReInvent-IDP-Insurance-Demo-Doc-Classifier";
        private const string DOCUMENT_CLASSIFIER_VERSION = "IDP-v1";
        //private const string DOCUMENT_CLASSIFIER_ENDPOINT_NAME = "ReInvent-IDP-Demo-Endpoint";
        private const string DOCUMENT_CLASSIFIER_ENDPOINT_NAME = "ReInvent-IDP-Insurance-Demo-Endpoint";
        
        public IDPModel(IAmazonComprehend comprehendClient, IAmazonTextract textractClient, IWebHostEnvironment hostEnvironment)
        {
            _comprehendClient = comprehendClient;
            _textractClient = textractClient;
            _hostEnvironment = hostEnvironment;
        }

        public void OnGet()
        {
        }

        private async Task<string> GetDocumentLines(string documentKey)
        {
            var request = new DetectDocumentTextRequest
            {
                Document = new Document
                {
                    S3Object = new S3Object
                    {
                        Bucket = BUCKET_NAME,
                        Name = documentKey
                    }
                }
            };
            var response = await _textractClient.DetectDocumentTextAsync(request);
            
            var lines = new StringBuilder();
            foreach (var block in response.Blocks)
            {
                if (block.BlockType == Amazon.Textract.BlockType.LINE)
                {
                    lines.Append($"{block.Text}\n");
                }
            }
            
            return lines.ToString();
        }

        public async Task OnPostPrepareDataAsync()
        {
            var csvFileName = "classifier-training.csv";
            var fullCsvFileName = System.IO.Path.Combine(_hostEnvironment.WebRootPath, "uploads", csvFileName);
            
            var s3 = new Amazon.S3.AmazonS3Client();
            
            // check if training CSV already exists
            if (!System.IO.File.Exists(fullCsvFileName))
            {
                var csv = new StringBuilder();
                
                // list files in S3 bucket
                var listRequest = new Amazon.S3.Model.ListObjectsRequest
                {
                    BucketName = BUCKET_NAME,
                    Prefix = "classification-training"
                };
                var listResponse = await s3.ListObjectsAsync(listRequest);
                var files = listResponse.S3Objects.Select(x => x.Key);
                foreach (var file in files)
                {
                    Console.WriteLine($"Processing file: {file}");

                    var lines = await GetDocumentLines(file);
                    lines = lines.Replace('"', '\'');

                    if (file.StartsWith("classification-training/receipts/"))
                    {
                        csv.AppendLine($"receipt,\"{lines}\"");
                    }
                    else if (file.StartsWith("classification-training/bank-statements/"))
                    {
                        csv.AppendLine($"bank-statement,\"{lines}\"");
                    }
                    else if (file.StartsWith("classification-training/invoices/"))
                    {
                        csv.AppendLine($"invoice,\"{lines}\"");
                    }
                }

                // save file to disk (not to generate again)
                using (var stream = new FileStream(fullCsvFileName, FileMode.Create))
                {
                    var bytes = Encoding.UTF8.GetBytes(csv.ToString());
                    await stream.WriteAsync(bytes, 0, bytes.Length);
                }
                
            }

            // upload training CSV to S3 bucket
            var putRequest = new Amazon.S3.Model.PutObjectRequest
            {
                BucketName = BUCKET_NAME,
                Key = csvFileName,
                FilePath = fullCsvFileName,
                
            };
            await s3.PutObjectAsync(putRequest);

        }

        public async Task OnPostCreateClassifierAsync()
        {             
            var client = new AmazonSecurityTokenServiceClient();
            var response = await client.GetCallerIdentityAsync(new GetCallerIdentityRequest()); 
            var account = response.Account;
            
            var dataAccessRoleArn = $"arn:aws:iam::{account}:role/service-role/AmazonComprehendServiceRole-IDP-access-S3"; // give access to list/read from S3 bucket

            var classifiers = await _comprehendClient.ListDocumentClassifierSummariesAsync(new ListDocumentClassifierSummariesRequest());
            // check if classifier not already exists
            if (!classifiers.DocumentClassifierSummariesList.Exists(classifier => classifier.DocumentClassifierName == DOCUMENT_CLASSIFIER_NAME))
            {
                // create new classifier
                var classifierRequest = new CreateDocumentClassifierRequest
                {
                    LanguageCode = "en",
                    Mode = DocumentClassifierMode.MULTI_CLASS,
                    DocumentClassifierName = DOCUMENT_CLASSIFIER_NAME,
                    VersionName = DOCUMENT_CLASSIFIER_VERSION,
                    DataAccessRoleArn = dataAccessRoleArn,
                    InputDataConfig = new DocumentClassifierInputDataConfig
                    {
                        DataFormat = DocumentClassifierDataFormat.COMPREHEND_CSV,
                        //S3Uri = $"s3://{BUCKET_NAME}/classifier-training.csv"
                        S3Uri = $"s3://{BUCKET_NAME}/insurance_comprehend_train_data.csv"
                    }
                };
                var classifierResponse = await _comprehendClient.CreateDocumentClassifierAsync(classifierRequest);
                var classifierArn = classifierResponse.DocumentClassifierArn; // can be used to check status of the training job
            }
            else
            {
                var modelArn = $"arn:aws:comprehend:eu-west-1:{account}:document-classifier/{DOCUMENT_CLASSIFIER_NAME}/version/{DOCUMENT_CLASSIFIER_VERSION}";
                // classifier already exists, create real-time endpoint, if does not exist
                var endpoints = await _comprehendClient.ListEndpointsAsync(new ListEndpointsRequest());
                if (!endpoints.EndpointPropertiesList.Exists(endpoint => endpoint.ModelArn == modelArn))
                {
                    // create endpoint
                    var createEndpointRequest = new CreateEndpointRequest
                    {
                        DesiredInferenceUnits = 1,
                        DataAccessRoleArn = dataAccessRoleArn,
                        ModelArn = modelArn,
                        EndpointName = DOCUMENT_CLASSIFIER_ENDPOINT_NAME
                    };
                    await _comprehendClient.CreateEndpointAsync(createEndpointRequest);
                }
            }
        }

        public async Task OnPostDetectDocumentTypeAsync()
        {
            if (FormFile == null)
                return;
            
            var client = new AmazonSecurityTokenServiceClient();
            var response = await client.GetCallerIdentityAsync(new GetCallerIdentityRequest()); 
            var account = response.Account;
            
            // save document image to display it
            FileName = $"{Guid.NewGuid().ToString()}{System.IO.Path.GetExtension(FormFile.FileName)}";
            var fullFileName = System.IO.Path.Combine(_hostEnvironment.WebRootPath, "uploads", FileName);

            await using (var stream = new FileStream(fullFileName, FileMode.Create))
            {
                await FormFile.CopyToAsync(stream);
            }

            var memoryStream = new MemoryStream();
            await FormFile.CopyToAsync(memoryStream);

            // await _comprehendClient.StartDocumentClassificationJobAsync(new StartDocumentClassificationJobRequest
            // {
            //     InputDataConfig = new InputDataConfig
            //     {
            //         InputFormat = InputFormat.ONE_DOC_PER_FILE,
            //         DocumentReaderConfig = new DocumentReaderConfig
            //         {
            //             DocumentReadAction = DocumentReadAction.TEXTRACT_DETECT_DOCUMENT_TEXT,
            //             DocumentReadMode = DocumentReadMode.FORCE_DOCUMENT_READ_ACTION
            //         },
            //         S3Uri = ""
            //     }
            // });
            
            var classifyDocumentRequest = new ClassifyDocumentRequest
            {
                Bytes = memoryStream,
                EndpointArn = $"arn:aws:comprehend:eu-west-1:{account}:document-classifier-endpoint/{DOCUMENT_CLASSIFIER_ENDPOINT_NAME}",
                DocumentReaderConfig = new DocumentReaderConfig
                {
                    DocumentReadAction = DocumentReadAction.TEXTRACT_DETECT_DOCUMENT_TEXT,
                    DocumentReadMode = DocumentReadMode.FORCE_DOCUMENT_READ_ACTION
                }
            };

            var classifyDocumentResponse = await _comprehendClient.ClassifyDocumentAsync(classifyDocumentRequest);

            var stringBuilder = new StringBuilder();
            
            foreach (var documentClass in classifyDocumentResponse.Classes)
            {
                stringBuilder.AppendFormat(
                    "Type: <b>{0}</b>, Confidence score: <b>{1}</b><br>",
                    documentClass.Name,
                    documentClass.Score);
            }

            Result = stringBuilder.ToString();
        }
        
    }
}
