namespace News.API.Controllers
{
    [Authorize(Roles = "User")]
    public class SentimentController(ISentimentService _newsService,IUserService _userService) : ApiController
    {
		// GET: api/sentiment/{sentiment}
		[HttpGet("{sentiment}")]
        public async Task<IActionResult> GetSentimentNews(string sentiment)
        {
            sentiment = sentiment.ToLower();

            if (sentiment != "positive" && sentiment != "negative")
                return BadRequest("Sentiment must be either 'positive' or 'negative'");

            var user = await _userService.GetCurrentUserAsync();
            if (user == null)
                return Unauthorized("User not found.");
            var articles = await _newsService.GetNewsBySentimentAsync(sentiment, user.Id);
            return Ok(articles);
        }
    }
}
