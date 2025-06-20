namespace News.API.Controllers
{
    [Authorize(Roles ="Admin,User")]
    public class CommentController(ICommentService _commentService,
        IUserService _userService ,INewsService _newsService) : ApiController
    {
        // POST: api/comment/{newsId}/comments
        [HttpPost("{newsId}/comments")]
        public async Task<IActionResult> AddComment(string newsId, [FromBody] AddCommentDto model)
        {
            var article = await _newsService.GetNewsByIdAsync(newsId);
            if (article == null)
                return NoContent(); 
            var user = await _userService.GetCurrentUserAsync();
            var comment = new Comment
            {
                Content = model.Content,
                UserId = user.Id,
                User = user,
                ArticleId = newsId,
                CreatedAt = DateTime.UtcNow,
            };
            await _commentService.AddAsync(comment);
            return Ok(new { result = "Comment added" });
        }
        // PUT: api/comment/comments/{id}
        [HttpPut("comments/{id}")]
        public async Task<IActionResult> EditComment(int id, [FromBody] UpdateCommentDto model)
        {
            var user = await _userService.GetCurrentUserAsync();
            var comment = await _commentService.GetByIdAsync(id);
            if (comment == null)
                return NoContent();
            if (comment.UserId != user.Id)
                return Forbid("You are not authorized to edit this comment.");

            comment.Content = model.Content;
            comment.CreatedAt = DateTime.UtcNow;
            await _commentService.UpdateAsync(comment);
            return Ok(new { message = "Comment updated successfully." });
        }

        // DELETE: api/comment/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComment(int id)
        {
            var comment = await _commentService.GetByIdAsync(id);
            if (comment == null)
                return NoContent();

            await _commentService.DeleteAsync(id);
            return Ok(new { result = "Comment deleted" });
        }

        // GET: api/comment
        [HttpGet]
        public async Task<IActionResult> GetAllComments()
        {
            var comments = await _commentService.GetAllAsync();
            var formattedComments = comments.Select(c => new CommentDto
            {
                Id = c.Id,
                Content = c.Content,
                CreatedAt = c.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"), 
                UserId = c.UserId,
                UserName = c.User.UserName,
                IsLocked = c.User.LockoutEnd.HasValue && c.User.LockoutEnd > DateTimeOffset.UtcNow,
                ProfilePicUrl = c.User.ProfilePicUrl,
                ContainsBadWord = c.ContainsBadWords
            });
            return Ok(formattedComments);
        }

        // GET: api/comment/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCommentById(int id)
        {
            var comment = await _commentService.GetByIdAsync(id);
            if (comment == null)
                return NoContent();

            var formattedComment = new CommentDto
            {
                Id = comment.Id,
                Content = comment.Content,
                CreatedAt = comment.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"), 
                UserId = comment.UserId,
                UserName = comment.User.UserName,
                IsLocked = comment.User.LockoutEnd.HasValue && comment.User.LockoutEnd > DateTimeOffset.UtcNow,
                ProfilePicUrl = comment.User.ProfilePicUrl,
                ContainsBadWord = comment.ContainsBadWords
                
            };
            return Ok(formattedComment);
        }

        // GET: api/comment/user/{userId}
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetCommentsByUserId(string userId)
        {
            var comments = await _commentService.GetCommentsByUserIdAsync(userId);
            if (comments == null || !comments.Any())
                return NoContent();

            var formattedComments = comments.Select(c => new CommentDto
            {
                Id = c.Id,
                Content = c.Content,
                CreatedAt = c.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"), 
                UserId = c.UserId,
                UserName = c.User.UserName,
                IsLocked = c.User.LockoutEnd.HasValue && c.User.LockoutEnd > DateTimeOffset.UtcNow,
                ProfilePicUrl = c.User.ProfilePicUrl,
                ContainsBadWord = c.ContainsBadWords
            });

            return Ok(formattedComments);
        }

        // GET: api/comment/article/{articleId}
        [HttpGet("article/{articleId}")]
        public async Task<IActionResult> GetCommentsByArticleId(string articleId)
        {
            var comments = await _commentService.GetCommentsByArticleIdAsync(articleId);
            if (comments == null || !comments.Any())
                return NoContent();

            var formattedComments = comments.Select(c => new CommentDto
            {
                Id = c.Id,
                Content = c.Content,
                CreatedAt = c.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"), 
                UserId = c.UserId,
                UserName = c.User?.UserName??"N/A",
                IsLocked = c.User?.LockoutEnd.HasValue ?? false && c.User.LockoutEnd > DateTimeOffset.UtcNow,
                ProfilePicUrl = c.User?.ProfilePicUrl ?? string.Empty,
                ContainsBadWord = c.ContainsBadWords
            });

            return Ok(formattedComments);
        }

        //POST : api/comment/moderate
        [HttpPost("moderate")]
        public async Task<IActionResult> ModerateComment([FromBody] CommentRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Comment))
            {
                return BadRequest("Comment cannot be empty.");
            }

            var filteredComment = await _commentService.FilterBadWordsAsync(request.Comment);
            return Ok(new { Original = request.Comment, Moderated = filteredComment });
        }
    }
}
