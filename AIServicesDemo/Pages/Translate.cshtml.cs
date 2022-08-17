using Amazon.Comprehend;
using Amazon.Comprehend.Model;
using Amazon.Translate;
using Amazon.Translate.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;

namespace AIServicesDemo.Pages
{
    public class TranslateModel : PageModel
    {
        [BindProperty]
        public string Text { get; set; }

        public string Result { get; set; }

        private readonly IAmazonComprehend _comprehendClient;
        private readonly IAmazonTranslate _translateClient;

        public TranslateModel(IAmazonComprehend comprehendClient, IAmazonTranslate translateClient)
        {
            _comprehendClient = comprehendClient;
            _translateClient = translateClient;
        }

        public void OnGet()
        {

        }

        public async Task OnPostTranslateAsync()
        {
            var request = new DetectDominantLanguageRequest()
            {
                Text = Text
            };

            var response = await _comprehendClient.DetectDominantLanguageAsync(request);

            var stringBuilder = new StringBuilder();

            var languageCode = response.Languages.First().LanguageCode;

            if (languageCode == "en")
            {
                stringBuilder.AppendLine("Input text is in English.");
            }
            else
            {
                stringBuilder.AppendFormat("Translating from <b>{0}</b>:<br>", languageCode);
                stringBuilder.AppendLine("==========================<br>");

                var translatRequest = new TranslateTextRequest
                {
                    Text = Text,
                    SourceLanguageCode = languageCode,
                    TargetLanguageCode = "en"
                };

                var translatResponse = await _translateClient.TranslateTextAsync(translatRequest);

                stringBuilder.Append(translatResponse?.TranslatedText);
            }

            Result = stringBuilder.ToString();
        }
    }
}
