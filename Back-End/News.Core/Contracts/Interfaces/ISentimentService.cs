namespace News.Core.Contracts.Interfaces
{
	public interface ISentimentService
	{
		Task<List<NewsArticle>> GetNewsBySentimentAsync(string sentiment, string userId);
	}
}
