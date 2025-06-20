namespace News.Core.Dtos.NewsCategory
{
    public class UserPreferencesDto
    {
        public string UserId { get; set; } = string.Empty;
        public ICollection<string> PreferredCategories { get; set; } = new List<string>();
    }
}
