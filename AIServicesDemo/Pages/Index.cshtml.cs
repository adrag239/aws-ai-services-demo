using Amazon.Comprehend;
using Amazon.Comprehend.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Translate;
using Amazon.Translate.Model;
using Amazon.S3;
using Amazon.S3.Model;

namespace AIServicesDemo.Pages
{
    public class IndexModel : PageModel
    {
        [BindProperty]
        public string Text { get; set; } = string.Empty; 
        public string Result { get; set; } = string.Empty;

        private readonly IAmazonComprehend _comprehendClient;
        private readonly IAmazonTranslate _translateClient;

        public IndexModel(IAmazonComprehend comprehendClient, IAmazonTranslate translateClient)
        {
            _comprehendClient = comprehendClient;
            _translateClient = translateClient;
        }

        public void OnGet()
        {
            
        }

        public async Task OnPostLanguageAsync()
        {
            // detect language of the text
            
        }

        public async Task OnPostEntitiesAsync()
        {
            var request = new DetectEntitiesRequest()
            {
                Text = Text,
                LanguageCode = "en"
            };

            var response = await _comprehendClient.DetectEntitiesAsync(request);

            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("Entities:<br>");
            stringBuilder.AppendLine("==========================<br>");

            foreach (var entity in response.Entities)
            {
                stringBuilder.AppendFormat(
                    "Text: <b>{0}</b>, Type: <b>{1}</b>, Score: <b>{2}</b>, Offset: {3}-{4}<br>",
                    entity.Text,
                    entity.Type,
                    entity.Score,
                    entity.BeginOffset,
                    entity.EndOffset);
            }

            Result = stringBuilder.ToString();
        }

        public async Task OnPostPIIAsync()
        {
            var request = new DetectPiiEntitiesRequest()
            {
                Text = Text,
                LanguageCode = "en"
            };

            var response = await _comprehendClient.DetectPiiEntitiesAsync(request);

            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("PII:<br>");
            stringBuilder.AppendLine("==========================<br>");

            foreach (var entity in response.Entities)
            {
                stringBuilder.AppendFormat(
                    "Text: <b>{0}</b>, Type: <b>{1}</b>, Score: <b>{2}</b>, Offset: {3}-{4}<br>",
                    Text.Substring(entity.BeginOffset, entity.EndOffset - entity.BeginOffset),
                    entity.Type,
                    entity.Score,
                    entity.BeginOffset,
                    entity.EndOffset);
            }

            Result = stringBuilder.ToString();

        }
    }
}