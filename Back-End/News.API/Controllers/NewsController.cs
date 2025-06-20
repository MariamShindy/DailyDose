namespace News.API.Controllers
{

    [Authorize(Roles = "User")]
    public class NewsController(INewsService _newsService) : ApiController
    {
        //GET : api/news/category/{topic}
        [HttpGet("category/{topic}")]
        public async Task<IActionResult> GetNewsByCategory(string topic, [FromQuery] string language = "en", [FromQuery] string country = "us")
        {
            var news = await _newsService.GetNewsByCategoryAsync(topic, language, country);
            return Ok(news);
        }

        //GET : api/news/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetNewsById(string id)
        {
            var news = await _newsService.GetNewsByIdAsync(id);
            if (news == null)
                return NoContent();
            return Ok(news);
        }

        //GET : api/news/categories
        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _newsService.GetCategoriesAsync();
            return Ok(categories);
        }

        //GET : api/news/generate-pdf/{id}
        [HttpGet("generate-pdf/{id}")]
        public async Task<IActionResult> GeneratePdf(string id)
        {
            var article = await _newsService.GetNewsByIdAsync(id); 
            if (article == null)
                return NotFound($"Article with ID {id} not found.");
            byte[] pdfBytes = _newsService.GenerateArticlePdf(article); 
            return File(pdfBytes, "application/pdf", $"{article.Title}.pdf");
        }
    }
}
