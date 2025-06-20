namespace News.Core.Contracts.Interfaces
{
	public interface IRecommendationService
	{
		Task<List<NewsArticle>> GetRecommendedArticlesAsync(List<string> topics, string userId);
        Task<List<NewsArticle>> GetLatestRecommendationsAsync(string userId, int pageNumber =0, int? pageSize = null);

    }
}
