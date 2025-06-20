namespace News.Service.Services
{
    public class SocialMediaService(INewsService _newsService , IConfiguration _configuration) : ISocialMediaService
    {
        private readonly string _facebookShareUrl = _configuration["ShareLinks:Facebook"]!;
        private readonly string _whatsappShareUrl = _configuration["ShareLinks:Whatsapp"]!;
        private readonly string _twitterShareUrl = _configuration["ShareLinks:Twitter"]!;
        public Dictionary<string, string> GenerateShareLinks(string newsId, string platform = null!)
        {
            var baseUrl = _newsService.GetNewsByIdAsync(newsId).Result.Link;
            var shareLinks = new Dictionary<string, string>();

            shareLinks["facebook"] = $"{_facebookShareUrl}?u={Uri.EscapeDataString(baseUrl)}";
            shareLinks["whatsapp"] = $"{_whatsappShareUrl}?text={Uri.EscapeDataString(baseUrl)}";
            shareLinks["twitter"] = $"{_twitterShareUrl}?url={Uri.EscapeDataString(baseUrl)}";

            if (platform != null && shareLinks.ContainsKey(platform.ToLower()))
            {
                return new Dictionary<string, string>
            {
                { platform.ToLower(), shareLinks[platform.ToLower()] }
            };
            }
            return shareLinks;
        }

    }
}
