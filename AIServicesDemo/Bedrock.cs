namespace AIServicesDemo
{
    public class CountPenalty
    {
        public int scale { get; set; }
    }

    public class FrequencyPenalty
    {
        public int scale { get; set; }
    }

    public class PresencePenalty
    {
        public int scale { get; set; }
    }

    public class BedrockRequest
    {
        public string prompt { get; set; }
        public int maxTokens { get; set; }
        public double temperature { get; set; }
        public double topP { get; set; }
        public List<object> stopSequences { get; set; }
        public CountPenalty countPenalty { get; set; }
        public PresencePenalty presencePenalty { get; set; }
        public FrequencyPenalty frequencyPenalty { get; set; }
    }
    
    public class Completion
    {
        public Data data { get; set; }
        public FinishReason finishReason { get; set; }
    }

    public class Data
    {
        public string text { get; set; }
        public List<Token> tokens { get; set; }
    }

    public class FinishReason
    {
        public string reason { get; set; }
    }

    public class GeneratedToken
    {
        public string token { get; set; }
        public double logprob { get; set; }
        public double raw_logprob { get; set; }
    }

    public class Prompt
    {
        public string text { get; set; }
        public List<Token> tokens { get; set; }
    }

    public class BedrockResponse
    {
        public int id { get; set; }
        public Prompt prompt { get; set; }
        public List<Completion> completions { get; set; }
    }

    public class TextRange
    {
        public int start { get; set; }
        public int end { get; set; }
    }

    public class Token
    {
        public GeneratedToken generatedToken { get; set; }
        public object topTokens { get; set; }
        public TextRange textRange { get; set; }
    }
}