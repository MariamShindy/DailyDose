namespace News.Core.Entities.TextToSpeech
{
    public class TextToSpeechRequest
    {
        public string Text { get; set; } = string.Empty;
        public string? Language { get; set; } = "en-US";
    }
}
