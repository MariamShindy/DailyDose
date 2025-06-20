namespace News.Service.Services
{
    public class TranslationService (HttpClient _httpClient , IConfiguration _configuration) : ITranslationService
	{
        public async Task<TranslationResponse> TranslateTextAsync(TranslationRequest request)
        {
            var payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                text = request.Text,
                source_lang = request.SourceLang,
                target_lang = request.TargetLang
            });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            _httpClient.Timeout = TimeSpan.FromMinutes(20);
			var flaskApiUrl = _configuration["FlaskApi:BaseUrl"];

			var response = await _httpClient.PostAsync($"{flaskApiUrl}/translate", content);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Translation failed: {response.StatusCode}");
            }
            var responseBody = await response.Content.ReadAsStringAsync();
            var responseJson = JsonDocument.Parse(responseBody);
            var translation = responseJson.RootElement.GetProperty("translation").GetString();
            return new TranslationResponse { Translation = translation };
        }
    }
}
