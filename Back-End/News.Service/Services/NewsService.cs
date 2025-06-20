namespace News.Service.Services
{
    public class NewsService(HttpClient _httpClient, IMapper _mapper, ILoggingService _logger,
        IUnitOfWork _unitOfWork, IConfiguration _configuration, IRecommendationService _recommendationService,
        IUserService _userService, IMemoryCache _cache) : INewsService
    {
        private readonly string _apiKey = _configuration["NewsCatcher:ApiKey"]!;
        private readonly string _baseUrl = _configuration["NewsCatcher:BaseUrl"]!;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        private readonly List<string> _categories = new()
        {
            "Business", "Economics", "Entertainment", "Finance", "Health", "Politics",
            "Science", "Sports", "Tech", "Crime", "Lifestyle", "Automotive", "Travel", "Weather", "General"
        };

        #region JSON Data Fetch

        public async Task<List<NewsArticle>> GetAllNewsAsync(List<string> categories, int pageNumber = 0, int? pageSize = null, string language = "en", string country = "us")
        {
            try
            {
                var articles = await ReadArticlesFromJsonAsync();
                var balancedArticles = BalanceArticlesByTopic(articles);

                if (pageSize is null || pageNumber == 0)
                    return LogAndReturnArticles("Returning all articles from json", balancedArticles);

                return PaginateArticles(categories, balancedArticles, pageNumber, pageSize.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(NewsService), "Error reading JSON file", ex);
                return [];
            }
        }

        private async Task<List<NewsArticle>> ReadArticlesFromJsonAsync()
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DataSeeding", "NewsData.json");
            var jsonData = await File.ReadAllTextAsync(filePath);
            return JsonConvert.DeserializeObject<List<NewsArticle>>(jsonData) ?? [];
        }

        private List<NewsArticle> BalanceArticlesByTopic(List<NewsArticle> articles) =>
            articles.GroupBy(a => a.Topic).SelectMany(g => g.Take(10)).ToList();

        private List<NewsArticle> PaginateArticles(List<string> categories, List<NewsArticle> articles, int pageNumber, int pageSize)
        {
            var paginated = articles.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
            var filtered = paginated.FindAll(a => categories.Contains(a.Topic));
            _logger.LogInfo(nameof(NewsService), $"Number of articles fetched from json ==> {filtered.Count}");
            return filtered;
        }

        private List<NewsArticle> LogAndReturnArticles(string message, List<NewsArticle> articles)
        {
            _logger.LogInfo(nameof(NewsService), $"{message} ==> {articles.Count}");
            return articles;
        }

        #endregion

        #region API v3 

        public async Task<List<NewsArticle>> GetNewsFromApiAsync(List<string> categories, int pageNumber = 1, int pageSize = 100, string language = "en", string country = "us")
        {
            try
            {
                var query = string.Join(" OR ", categories);
                var (from, to) = GetDateRangeForNews();
                var countries = string.Join(",", new[] { country, "EG", "CA", "FR", "GB", "DE" });

                var requestUrl = $"{_baseUrl}?q={query}&from_={from}&to_={to}&lang={language}&countries={countries}&page_size={pageSize}&page={pageNumber}";
                SetDefaultHeaders();

                var response = await _httpClient.GetStringAsync(requestUrl);
                var newsResponse = JsonConvert.DeserializeObject<NewsApiResponse>(response);

                return ProcessNewsApiResponse(newsResponse);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(nameof(NewsService), "Request failed", ex);
                return [];
            }
        }

        private void SetDefaultHeaders()
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("x-api-token", _apiKey);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        private (string from, string to) GetDateRangeForNews()
        {
            var currentDate = DateTime.UtcNow.Date;
            return (currentDate.AddDays(-10).ToString("yyyy-MM-dd"), currentDate.ToString("yyyy-MM-dd"));
        }

        private List<NewsArticle> ProcessNewsApiResponse(NewsApiResponse? newsResponse)
        {
            if (newsResponse?.Articles == null)
                return [];

            var articlesList = newsResponse.Articles.ToList();
            foreach (var article in articlesList)
            {
                article.Author ??= "Unknown author";
                article.Twitter_Account ??= "Unknown account";
                if (!article.Authors.Any())
                    article.Authors.Add("Unknown authors");
            }

            _logger.LogInfo(nameof(NewsService), $"Returning articles count ==> {articlesList.Count}");
            return articlesList;
        }

        #endregion

        #region Category Methods

        public async Task<List<string>> GetCategoriesAsync()
        {
            await AddCategoriesToDatabaseAsync(_categories);
            return _categories;
        }

        private async Task AddCategoriesToDatabaseAsync(List<string> categories)
        {
            var existing = await _unitOfWork.Repository<Category>().GetAllAsync();
            if (existing.Any()) return;

            foreach (var category in categories)
            {
                await AddCategoryAsync(new AddOrUpdateCategoryDto { Name = category });
                _logger.LogInfo(nameof(NewsService), $"Added {category} category to database.");
            }
        }

        private async Task<bool> AddCategoryAsync(AddOrUpdateCategoryDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ArgumentException("Category name cannot be null or empty.");

            await _unitOfWork.Repository<Category>().AddAsync(new Category { Name = dto.Name });
            return await _unitOfWork.CompleteAsync() > 0;
        }

        #endregion

        #region Cache + API News by Category

        public async Task<List<NewsArticle>> GetNewsByCategoryAsync(string category, string language = "en", string country = "us")
        {
            var articles = await GetNewsByOneCategory(category);
            return articles.Where(a => a.Topic?.Contains(category, StringComparison.OrdinalIgnoreCase) == true).ToList();
        }

        private async Task<List<NewsArticle>> GetNewsByOneCategory(string category)
        {
            string cacheKey = $"news_{category.Trim().ToLowerInvariant()}";
            if (_cache.TryGetValue(cacheKey, out List<NewsArticle> cached))
                return cached;

            await _semaphore.WaitAsync();
            try
            {
                if (_cache.TryGetValue(cacheKey, out cached))
                    return cached;

                var articles = await FetchNewsByCategoryFromApi(category);
                _cache.Set(cacheKey, articles, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(5) });
                return articles;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(nameof(NewsService), "API request failed", ex);
                _cache.Set(cacheKey, new List<NewsArticle>(), new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });
                return [];
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task<List<NewsArticle>> FetchNewsByCategoryFromApi(string category)
        {
            var (from, to) = GetDateRangeForNews();
            var countries = string.Join(",", new[] { "US", "EG", "CA", "FR", "GB", "DE" });
            var query = Uri.EscapeDataString(category);
            var url = $"{_baseUrl}?q={query}&from_={from}&to_={to}&lang=en&countries={countries}&page_size=100&page=1";

            SetDefaultHeaders();

            var response = await _httpClient.GetStringAsync(url);
            var newsResponse = JsonConvert.DeserializeObject<NewsApiResponse>(response);
            return ProcessNewsApiResponse(newsResponse);
        }
        
        public async Task<IEnumerable<NewsArticleDto>> GetArticlesByCategoriesAsync(IEnumerable<CategoryDto> preferredCategories)
        {
            var names = preferredCategories.Select(c => c.Name).ToList();
            var articles = await GetAllNewsAsync(names);
            var filtered = articles.FindAll(a => names.Contains(a.Topic));
            return _mapper.Map<IEnumerable<NewsArticleDto>>(filtered);
        }
        #endregion

        #region News by Id

        public async Task<NewsArticle> GetNewsByIdAsync(string id)
        {
            var user = await _userService.GetCurrentUserAsync();
            var recArticles = await _recommendationService.GetLatestRecommendationsAsync(user.Id);

            var article = recArticles.FirstOrDefault(a => a.Id == id) ?? await GetNewsFromApiByIdAsync(id);
            if (article == null)
                _logger.LogWarning(nameof(NewsService), $"Article with id : {id} not found");

            return article;
        }

        private async Task<NewsArticle?> GetNewsFromApiByIdAsync(string id)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}_by_link?ids={id}");
                request.Headers.Add("x-api-token", _apiKey);
                request.Headers.Add("Accept", "application/json");

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var newsResponse = JsonConvert.DeserializeObject<NewsApiResponse>(content);

                return FormatSingleNewsArticle(newsResponse?.Articles?.FirstOrDefault(), id);
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(NewsService), $"Error fetching article from API for ID: {id}", ex);
                return null;
            }
        }

        private NewsArticle? FormatSingleNewsArticle(NewsArticle? article, string id)
        {
            if (article == null)
            {
                _logger.LogWarning(nameof(NewsService), $"No article found for ID: {id}");
                return null;
            }

            article.Author ??= "Unknown author";
            article.Twitter_Account ??= "Unknown account";
            article.Authors ??= new List<string> { "Unknown authors" };

            return article;
        }

        #endregion

        #region PDF generation
        public byte[] GenerateArticlePdf(NewsArticle article)
        {
            using var memoryStream = new MemoryStream();
            using var writer = new PdfWriter(memoryStream);
            using var pdfDoc = new PdfDocument(writer);
            var document = new Document(pdfDoc);
            document.SetMargins(40, 40, 40, 40);

            BuildArticlePdfBody(document, article);
            document.Close();

            return memoryStream.ToArray();
        }

        private void BuildArticlePdfBody(Document document, NewsArticle article)
        {
            AddPdfTitle(document, article.Title);
            AddPdfImage(document, article.Media);
            AddPdfMetadata(document, article);
            AddSection(document, "Description", article.Description, 12, true);
            AddSection(document, "Content", article.Content, 12, true);
            AddPdfLink(document, article.Link);
        }

        private void AddPdfTitle(Document document, string? title)
        {
            document.Add(new Paragraph(title ?? "Untitled")
                .SetFontSize(24).SetBold()
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFontColor(ColorConstants.DARK_GRAY));
            document.Add(new LineSeparator(new SolidLine()));
            document.Add(new Paragraph("\n"));
        }

        private void AddPdfImage(Document document, string? media)
        {
            if (string.IsNullOrEmpty(media)) return;

            try
            {
                var imageData = ImageDataFactory.Create(media);
                var image = new Image(imageData).SetAutoScale(true)
                                                .SetHorizontalAlignment(HorizontalAlignment.CENTER);
                document.Add(image);
            }
            catch
            {
                document.Add(new Paragraph("Image unavailable").SetFontColor(ColorConstants.RED));
            }
            document.Add(new Paragraph("\n"));
        }

        private void AddPdfMetadata(Document document, NewsArticle article)
        {
            AddMetadata(document, "Authors", string.Join(", ", article.Authors ?? []));
            AddMetadata(document, "Topic", article.Topic);
            AddMetadata(document, "Country", article.Country);
            AddMetadata(document, "Published Date", article.Published_Date?.ToString());
            document.Add(new LineSeparator(new DottedLine()));
        }

        private void AddMetadata(Document document, string label, string? value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                document.Add(new Paragraph()
                    .Add(new Text($"{label}: ").SetBold().SetFontSize(16))
                    .Add(new Text(value).SetFontSize(16)));
            }
        }

        private void AddPdfLink(Document document, string? link)
        {
            if (!string.IsNullOrEmpty(link))
            {
                Link uri = new("Read more", PdfAction.CreateURI(link));
                document.Add(new Paragraph(uri.SetFontSize(12).SetUnderline().SetFontColor(ColorConstants.BLUE)));
            }

            document.Add(new Paragraph("\n"));
            document.Add(new Paragraph($"Generated by News Aggregator System on {DateTime.Now:yyyy-MM-dd}")
                .SetFontSize(10)
                .SetFontColor(ColorConstants.GRAY)
                .SetTextAlignment(TextAlignment.CENTER));
        }

        private void AddSection(Document document, string title, string? content, float fontSize, bool italic = false)
        {
            if (!string.IsNullOrEmpty(content))
            {
                string cleaned = Regex.Replace(content.Trim(), @"(\n\s*)+", "\n");
                var paragraph = new Paragraph()
                    .Add(new Text($"{title}: ").SetBold())
                    .Add(new Text(cleaned))
                    .SetFontSize(fontSize);
                if (italic) paragraph.SetItalic();
                document.Add(paragraph);
            }
        }

        #endregion
    }
}
