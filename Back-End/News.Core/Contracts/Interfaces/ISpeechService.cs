namespace News.Core.Contracts.Interfaces
{
    public interface ISpeechService
    {
        byte[] ConvertTextToSpeech(string text, string language = "en-US");
    }
}
