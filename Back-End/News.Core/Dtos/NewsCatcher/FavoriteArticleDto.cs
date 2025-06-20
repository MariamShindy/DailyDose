namespace News.Core.Dtos.NewsCatcher
{
    public class FavoriteArticleDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; }
        public string ArticleId { get; set; } = string.Empty;
        public DateTime AddedAt { get; set; }
    }
}
