using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;
using System.Text.Json;
using Amazon;
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;

namespace AIServicesDemo.Pages
{
    public class BedrockModel : PageModel
    {
        [BindProperty]
        public string Text { get; set; } = string.Empty; 
        public string Result { get; set; } = string.Empty;

        public void OnGet()
        {
            
        }

        public async Task OnPostSummaryAsync()
        {
            var runtime = new AmazonBedrockRuntimeClient(RegionEndpoint.USEast1);
            
            var bedrockRequest = new BedrockRequest
            {
                prompt = Text.ReplaceLineEndings(" "),
                maxTokens = 200,
                temperature = 0.3,
                topP = 1.0,
                countPenalty = new CountPenalty { scale = 0 },
                frequencyPenalty = new FrequencyPenalty {scale = 0 },
                presencePenalty = new PresencePenalty { scale = 0 },
                stopSequences = new List<object>()
            };
            
            // convert body to MemoryStream
            var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(bedrockRequest)));
            
            var request = new InvokeModelRequest
            {
                ModelId = "ai21.j2-ultra-v1",
                Accept = "*/*",
                ContentType = "application/json",
                Body = bodyStream
            };

            var response = await runtime.InvokeModelAsync(request);

            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("==========================<br>");
             
            // deserialize
            var bedrockResponse = JsonSerializer.Deserialize<BedrockResponse>(response.Body);
            stringBuilder.Append(bedrockResponse?.completions[0].data.text.Replace("*", "<br>- "));

            Result = stringBuilder.ToString();
        }

    }
}