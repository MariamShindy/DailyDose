namespace News.API.Controllers
{
    [Authorize(Roles ="User")]
    public class SpeechController(ISpeechService _textToSpeechService) : ApiController
    {
        //GET : api/Speech/text-to-speech
        [HttpPost("text-to-speech")]
        public IActionResult Speak([FromBody] TextToSpeechRequest request)
        {
            try
            {
                var audioBytes = _textToSpeechService.ConvertTextToSpeech(request.Text, request.Language);
                var stream = new MemoryStream(audioBytes);
                return new FileStreamResult(stream, "audio/wav")
                {
                    FileDownloadName = "speech.wav"
                };
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

    }
}

