using News.Core.Dtos.NewsCatcher;

namespace News.API.Controllers
{
    [Authorize(Roles = "User")]
    public class SearchController(ISearchService _searchService) : ApiController
    {
		// POST: api/search/searchResults
		[HttpPost("searchResults")]
        public async Task<IActionResult> SearchArticles([FromBody] SearchRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Query))
                return BadRequest(new { error = "No query provided" });
            try
            {
                var response = await _searchService.SearchArticlesAsync(request.Query);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
