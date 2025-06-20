namespace News.Service.Services
{
    public class RecommendationService(HttpClient _httpClient, IConfiguration _configuration) : IRecommendationService
    {
        private readonly string _cachedRecommendationsUrl = _configuration["FlaskApi:CachingRecommend"]!;
        private readonly string _recommendApiUrl = _configuration["FlaskApi:Recommend"]!;

        public async Task<List<NewsArticle>> GetRecommendedArticlesAsync(List<string> topics, string userId)
        {
            var requestBody = new { topics = topics, user_id = userId };
            var requestContent = new StringContent(System.Text.Json.JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_recommendApiUrl, requestContent);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Error from Recommendation API: {response.StatusCode}");
            }

            var result = await response.Content.ReadAsStringAsync();

            try
            {
                var recommendations = System.Text.Json.JsonSerializer.Deserialize<RecommendationResponse>(result, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return recommendations?.Recommendations ?? new List<NewsArticle>();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deserializing recommendation response: {ex.Message}", ex);
            }
        }
        public async Task<List<NewsArticle>> GetLatestRecommendationsAsync(string userId, int pageNumber = 0, int? pageSize = null)
        {
            var response = await _httpClient.GetAsync($"{_cachedRecommendationsUrl}?user_id={userId}");

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Error fetching cached recommendations: {response.StatusCode}");
            }
            var result = await response.Content.ReadAsStringAsync();

            try
            {
                var allRecommendations = System.Text.Json.JsonSerializer.Deserialize<List<NewsArticle>>(result, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<NewsArticle>();

                int index = 0;
                foreach (var article in allRecommendations)
                {
                    index++;
                }

                if (!pageSize.HasValue || pageNumber <= 0)
                    return allRecommendations;

                return allRecommendations
                    .Skip((pageNumber - 1) * pageSize.Value)
                    .Take(pageSize.Value)
                    .DistinctBy(a => a.Title)
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Deserialization Error] Full JSON: " + result);
                Console.WriteLine("[Deserialization Error] Message: " + ex.Message);
                Console.WriteLine("[Deserialization Error] StackTrace: " + ex.StackTrace);
                throw new Exception($"Error deserializing cached recommendations: {ex.Message}", ex);
            }
        }
    }
}