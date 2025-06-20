namespace News.Core.Contracts.Interfaces
{
    public interface ICommentService
    {
        Task<IEnumerable<Comment>> GetAllAsync();
        Task<Comment> GetByIdAsync(int id);
        Task AddAsync(Comment comment);
        Task UpdateAsync(Comment comment);
        Task DeleteAsync(int id);
        Task<IEnumerable<Comment>> GetCommentsByUserIdAsync(string userId);
        Task<IEnumerable<Comment>> GetCommentsByArticleIdAsync(string articleId);
        Task<(string FilteredComment, bool ContainsBadWords)> FilterBadWordsAsync(string comment);
    }
}
