namespace News.Core.Dtos.NewsCategory
{
    public class SetPreferredCategoriesDto
    {
        public ICollection<string> CategoryNames { get; set; } = new List<string>();
    }
}
