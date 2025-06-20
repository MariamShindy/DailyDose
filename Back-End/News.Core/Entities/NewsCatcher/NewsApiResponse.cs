namespace News.Core.Entities.NewsCatcher
{
    public class NewsApiResponse
    {
        public string? Status { get; set; }
        public int TotalResults { get; set; }
        public List<NewsArticle> Articles { get; set; } = new List<NewsArticle>();
    }
}
