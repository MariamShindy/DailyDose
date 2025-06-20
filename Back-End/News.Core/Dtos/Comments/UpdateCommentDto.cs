namespace News.Core.Dtos.Comments
{
    public class UpdateCommentDto
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}
