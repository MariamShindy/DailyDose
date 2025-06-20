namespace News.Core.Contracts.Interfaces
{
    public interface IFavoriteService
    {
        Task AddToFavoritesAsync(string userId, string articleId);
        Task<IEnumerable<NewsArticle>> GetFavoritesByUserAsync(string userId);
        Task RemoveFromFavoritesAsync(string userId, string articleId);
        Task<bool> IsArticleFavoritedAsync(string userId, string articleId);
        Task<UserFavoriteArticle> GetFavoriteByIdAsync(int favoriteId);
    }
}
