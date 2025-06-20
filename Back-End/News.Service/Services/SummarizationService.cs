namespace News.Service.Services
{
    public class SummarizationService(HttpClient _httpClient , IConfiguration _configuration) : ISummarizationService
	{
        private readonly string _flaskApiUrl = _configuration["FlaskApi:Summarize"]!;
		public async Task<string> SummarizeTextAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Text should not be empty", nameof(text));

            var jsonContent = new StringContent(System.Text.Json.JsonSerializer.Serialize(new { text }), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_flaskApiUrl, jsonContent);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Error from Flask API: {response.StatusCode}");
            }

            var result = await response.Content.ReadAsStringAsync();
            var summaryResponse = System.Text.Json.JsonSerializer.Deserialize<SummarizationResponse>(result);
            return summaryResponse?.Summary ?? "Error in summarization";
        }
    }
}