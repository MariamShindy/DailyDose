using System.Linq.Expressions;

namespace News.Service.Services
{
    public class CommentService(ILoggingService _logger, IUnitOfWork _unitOfWork,
        HttpClient _httpClient, IConfiguration _configuration) : ICommentService
    {
        public async Task<IEnumerable<Comment>> GetAllAsync()
        {
            _logger.LogInfo(nameof(CommentService), "GetAll called");
            return await GetAllWithUserInclude();
        }

        public async Task<Comment> GetByIdAsync(int id)
        {
            _logger.LogInfo(nameof(CommentService), $"GetById with id: {id} called");
            return await GetCommentByConditionAsync(c => c.Id == id) ?? new Comment();
        }

        public async Task<IEnumerable<Comment>> GetCommentsByUserIdAsync(string userId)
        {
            _logger.LogInfo(nameof(CommentService), $"GetCommentsByUserIdAsync with userId: {userId} called");
            return await GetManyCommentsByConditionAsync(c => c.UserId == userId);
        }

        public async Task<IEnumerable<Comment>> GetCommentsByArticleIdAsync(string articleId)
        {
            _logger.LogInfo(nameof(CommentService), $"GetCommentsByArticleIdAsync with articleId: {articleId} called");
            return await GetManyCommentsByConditionAsync(c => c.ArticleId == articleId);
        }

        public async Task AddAsync(Comment comment)
        {
            (comment.Content, comment.ContainsBadWords) = await FilterBadWordsAsync(comment.Content);
            await _unitOfWork.Repository<Comment>().AddAsync(comment);
            await _unitOfWork.CompleteAsync();
        }

        public async Task UpdateAsync(Comment comment)
        {
            _logger.LogInfo(nameof(CommentService), "Update called");

            var existingComment = await _unitOfWork.Repository<Comment>().GetByIdAsync(comment.Id);
            if (existingComment == null)
            {
                _logger.LogWarning(nameof(CommentService), "Update --> existingComment not found");
                return;
            }

            (existingComment.Content, existingComment.ContainsBadWords) = await FilterBadWordsAsync(comment.Content);

            _unitOfWork.Repository<Comment>().Update(existingComment);
            await _unitOfWork.CompleteAsync();

            _logger.LogInfo(nameof(CommentService), "Update succeeded");
        }

        public async Task DeleteAsync(int id)
        {
            _logger.LogInfo(nameof(CommentService), $"Delete with id: {id} called");

            var comment = await _unitOfWork.Repository<Comment>().GetByIdAsync(id);
            if (comment == null) return;

            _unitOfWork.Repository<Comment>().Delete(comment);
            await _unitOfWork.CompleteAsync();

            _logger.LogInfo(nameof(CommentService), $"Delete with id: {id} succeeded");
        }

        public async Task<(string FilteredComment, bool ContainsBadWords)> FilterBadWordsAsync(string comment)
        {
            var requestData = PrepareBadWordFilterRequest(comment);
            var response = await _httpClient.PostAsync(_configuration["NeutrinoAPI:BaseUrl"], requestData);
            var jsonResponse = await response.Content.ReadAsStringAsync();

            _logger.LogInfo(nameof(CommentService), $"Neutrino API Response: {jsonResponse}");

            return ParseBadWordsFromResponse(jsonResponse, comment);
        }

        private async Task<IEnumerable<Comment>> GetAllWithUserInclude()
        {
            return await _unitOfWork.Repository<Comment>()
                .GetAllAsync(query => query.Include(c => c.User));
        }

        private async Task<Comment?> GetCommentByConditionAsync(Expression<Func<Comment, bool>> predicate)
        {
            var result = await _unitOfWork.Repository<Comment>()
                .FindAsync(predicate, query => query.Include(c => c.User));
            return result.FirstOrDefault();
        }

        private async Task<IEnumerable<Comment>> GetManyCommentsByConditionAsync(Expression<Func<Comment, bool>> predicate)
        {
            return await _unitOfWork.Repository<Comment>()
                .FindAsync(predicate, query => query.Include(c => c.User));
        }

        private FormUrlEncodedContent PrepareBadWordFilterRequest(string content)
        {
            var data = new Dictionary<string, string>
            {
                { "user-id", _configuration["NeutrinoAPI:UserID"] },
                { "api-key", _configuration["NeutrinoAPI:APIKey"] },
                { "content", content }
            };
            return new FormUrlEncodedContent(data);
        }

        private (string, bool) ParseBadWordsFromResponse(string jsonResponse, string originalContent)
        {
            using var doc = JsonDocument.Parse(jsonResponse);
            var root = doc.RootElement;
            bool containsBadWords = false;
            string filtered = originalContent;

            if (root.TryGetProperty("bad-words-list", out var badWordsList))
            {
                var badWords = badWordsList.EnumerateArray().Select(w => w.GetString()).Where(w => w != null);
                foreach (var word in badWords)
                {
                    filtered = Regex.Replace(filtered, $@"\b{Regex.Escape(word!)}\b", "****", RegexOptions.IgnoreCase);
                    containsBadWords = true;
                }
            }

            return (filtered, containsBadWords);
        }
    }
}
