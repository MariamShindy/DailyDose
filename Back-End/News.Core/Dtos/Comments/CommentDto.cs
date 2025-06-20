namespace News.Core.Dtos.Comments
{
    public class CommentDto
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public bool IsLocked { get; set; }
        public string ProfilePicUrl { get; set; } = string.Empty;
        public bool ContainsBadWord { get; set; }
    }
}
