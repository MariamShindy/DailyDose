namespace News.Service.Services
{
    public class FavoriteService(ILoggingService _logger,INewsService _newsService,
        IMemoryCache _cache, IUnitOfWork _unitOfWork) : IFavoriteService
    {
        private const string CacheKeyPrefix = "FavoriteArticles_";
        private const string CachedKeysKey = "cachedArticleKeys";

        public async Task AddToFavoritesAsync(string userId, string articleId)
        {
            _logger.LogInfo(nameof(FavoriteService), $"AddToFavorites called for userId: {userId} and articleId: {articleId}");

            var favorite = new UserFavoriteArticle
            {
                UserId = userId,
                ArticleId = articleId,
                AddedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<UserFavoriteArticle>().AddAsync(favorite);
            await _unitOfWork.CompleteAsync();

            await CacheArticleIfNotExistsAsync(articleId);
        }

        public async Task<IEnumerable<NewsArticle>> GetFavoritesByUserAsync(string userId)
        {
            _logger.LogInfo(nameof(FavoriteService), $"GetFavoritesByUser called for userId: {userId}");

            var favorites = await _unitOfWork.Repository<UserFavoriteArticle>().GetAllAsync();
            var articleIds = favorites.Where(f => f.UserId == userId).Select(f => f.ArticleId).ToList();

            var articles = new List<NewsArticle>();
            foreach (var articleId in articleIds)
            {
                var article = await GetOrCacheArticleAsync(articleId);
                if (article != null)
                {
                    articles.Add(article);
                }
            }

            _logger.LogInfo(nameof(FavoriteService), $"Total favorite articles fetched for user {userId}: {articles.Count}");
            return articles;
        }

        public async Task RemoveFromFavoritesAsync(string userId, string articleId)
        {
            _logger.LogInfo(nameof(FavoriteService), $"RemoveFromFavorites called for userId: {userId} and articleId: {articleId}");

            var favorite = await _unitOfWork.Repository<UserFavoriteArticle>()
                .FindAsync(f => f.UserId == userId && f.ArticleId == articleId);

            if (favorite != null)
            {
                _unitOfWork.Repository<UserFavoriteArticle>().Delete(favorite.FirstOrDefault());
                await _unitOfWork.CompleteAsync();

                var cacheKey = BuildUserArticleCacheKey(userId, articleId);
                _cache.Remove(cacheKey);

                _logger.LogInfo(nameof(FavoriteService), $"Article {articleId} removed from favorites and cache for userId: {userId}");
            }
        }

        public async Task<bool> IsArticleFavoritedAsync(string userId, string articleId)
        {
            _logger.LogInfo(nameof(FavoriteService), $"IsArticleFavorited called for userId: {userId} and articleId: {articleId}");

            var cacheKey = BuildUserArticleCacheKey(userId, articleId);
            if (_cache.TryGetValue(cacheKey, out _))
            {
                _logger.LogInfo(nameof(FavoriteService), $"Article {articleId} found in cache for userId: {userId}");
                return true;
            }

            var favorites = await _unitOfWork.Repository<UserFavoriteArticle>().GetAllAsync();
            var isFavorited = favorites.Any(f => f.UserId == userId && f.ArticleId == articleId);

            if (isFavorited)
            {
                _logger.LogInfo(nameof(FavoriteService), $"Article {articleId} found in database for userId: {userId}");
                var article = await _newsService.GetNewsByIdAsync(articleId);
                if (article != null)
                {
                    _cache.Set(cacheKey, article, TimeSpan.FromDays(2));
                    _logger.LogInfo(nameof(FavoriteService), $"Article {articleId} cached for userId: {userId}");
                }
            }

            return isFavorited;
        }

        public async Task<UserFavoriteArticle> GetFavoriteByIdAsync(int favoriteId)
        {
            _logger.LogInfo(nameof(FavoriteService), $"GetFavoriteById called with favoriteId: {favoriteId}");
            return await _unitOfWork.Repository<UserFavoriteArticle>().GetByIdAsync(favoriteId);
        }
        private string BuildUserArticleCacheKey(string userId, string articleId)
        {
            return $"{CacheKeyPrefix}{userId}_{articleId}";
        }

        private string BuildArticleCacheKey(string articleId)
        {
            return $"{CacheKeyPrefix}{articleId}";
        }

        private async Task CacheArticleIfNotExistsAsync(string articleId)
        {
            var cacheKey = BuildArticleCacheKey(articleId);

            if (!_cache.TryGetValue(cacheKey, out _))
            {
                var article = await _newsService.GetNewsByIdAsync(articleId);
                if (article != null)
                {
                    _cache.Set(cacheKey, article, TimeSpan.FromDays(1));
                    _logger.LogInfo(nameof(FavoriteService), $"Article cached with key: {cacheKey}");

                    var cachedKeys = _cache.Get<List<string>>(CachedKeysKey) ?? new List<string>();
                    cachedKeys.Add(cacheKey);
                    _cache.Set(CachedKeysKey, cachedKeys, TimeSpan.FromDays(1));
                }
            }
        }

        private async Task<NewsArticle?> GetOrCacheArticleAsync(string articleId)
        {
            var cacheKey = BuildArticleCacheKey(articleId);

            if (_cache.TryGetValue(cacheKey, out NewsArticle cachedArticle))
            {
                return cachedArticle;
            }

            var article = await _newsService.GetNewsByIdAsync(articleId);
            if (article != null)
            {
                _cache.Set(cacheKey, article, TimeSpan.FromDays(1));
                _logger.LogInfo(nameof(FavoriteService), $"Article {articleId} fetched and cached.");
            }
            else
            {
                _logger.LogWarning(nameof(FavoriteService), $"Article {articleId} not found in external API.");
            }

            return article;
        }
    }
}
