using Amazon.Comprehend;
using Amazon.Comprehend.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;

namespace AIServicesDemo.Pages
{
    public class IndexModel : PageModel
    {
        [BindProperty]
        public string Description { get; set; }

        public string Result { get; set; }

        private readonly IAmazonComprehend _comprehendClient;

        public IndexModel(IAmazonComprehend comprehendClient)
        {
            _comprehendClient = comprehendClient;
        }

        public void OnGet()
        {

        }

        public async Task OnPostLanguageAsync()
        {
            var request = new DetectDominantLanguageRequest()
            {
                Text = Description
            };

            var response = await _comprehendClient.DetectDominantLanguageAsync(request);

            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("Dominant Language:<br>");
            stringBuilder.AppendLine("==========================<br>");

            foreach (var dominantLanguage in response.Languages)
            {
                stringBuilder.AppendFormat(
                    "Language Code: <b>{0}</b>, Score: <b>{1}</b><br>",
                    dominantLanguage.LanguageCode,
                    dominantLanguage.Score);
            }

            Result = stringBuilder.ToString();
        }

        public async Task OnPostEntitiesAsync()
        {
            var request = new DetectEntitiesRequest()
            {
                Text = Description,
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
                Text = Description,
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
                    Description.Substring(entity.BeginOffset, entity.EndOffset - entity.BeginOffset),
                    entity.Type,
                    entity.Score,
                    entity.BeginOffset,
                    entity.EndOffset);
            }

            Result = stringBuilder.ToString();

        }
    }
}