namespace News.Core.Contracts.Interfaces
{
    public interface ISocialMediaService
    {
        Dictionary<string, string> GenerateShareLinks(string newsId, string platform = null!);
    }
}
