using System.Speech.Synthesis;

namespace News.Service.Services
{
    public class SpeechService(ILogger<SpeechService> _logger) : ISpeechService
    {
        public byte[] ConvertTextToSpeech(string text, string language = "en-US")
        {
            _logger.LogInformation("TextToSpeechService --> ConvertTextToSpeech Started");

            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Text cannot be empty.");

            using (var synth = new SpeechSynthesizer())
            using (var memoryStream = new MemoryStream())
            {
                SetVoice(synth, language);
                synth.SetOutputToWaveStream(memoryStream);
                synth.Speak(text);

                return memoryStream.ToArray();
            }
        }

        private void SetVoice(SpeechSynthesizer synth, string language)
        {
            var availableVoices = synth.GetInstalledVoices()
                                       .Where(v => v.VoiceInfo.Culture.Name.StartsWith(language))
                                       .ToList();
            foreach (var voice in synth.GetInstalledVoices())
            {
                _logger.LogInformation($"Voice: {voice.VoiceInfo.Name}, Language: {voice.VoiceInfo.Culture}");
            }

            if (availableVoices.Any())
                synth.SelectVoice(availableVoices.First().VoiceInfo.Name);
            else
                throw new InvalidOperationException($"No installed voices found for language: {language}");
        }
    }
}
