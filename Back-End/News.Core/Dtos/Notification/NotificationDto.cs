namespace News.Core.Dtos.Notification
{
    public class NotificationDto
    {
        public int Id { get; set; }
        public string ApplicationUserId { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string ArticleTitle { get; set; } = string.Empty;
        public string ArticleDescription { get; set; } = string.Empty;
        public string ArticleUrl { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string ArticleId { get; set; } = string.Empty;
    }
}
