namespace News.Service.Services
{
    public class NotificationService(ILoggingService _logger,IMailSettings _mailSettings,
        IUserService _userService,INewsService _newsService,
        IMapper _mapper,UserManager<ApplicationUser> _userManager,
        IUnitOfWork _unitOfWork) : INotificationService
    {
        public async Task SendNotificationsAsync()
        {
            _logger.LogInfo(nameof(NotificationService), "Start Sending notifications.");

            try
            {
                var users = await _userService.GetAllUsersAsync();

                foreach (var user in users)
                {
                    await NotifyUserAsync(user);
                }

                _logger.LogInfo(nameof(NotificationService), "Finished sending notifications.");
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(NotificationService), "An error occurred while sending notifications", ex);
            }
        }

        private async Task NotifyUserAsync(UserDto user)
        {
            try
            {
                var article = await GetRandomArticleForUser(user.Id);

                if (article is null)
                    return;

                var notificationDto = CreateNotificationDto(user.Id, article);
                var notification = _mapper.Map<Notification>(notificationDto);

                await SaveNotificationAsync(notification);
                await SendEmailAsync(notificationDto);

                _logger.LogInfo(nameof(NotificationService), $"Notification sent at {notificationDto.CreatedAt} to user with Email: {notificationDto.ApplicationUserId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(NotificationService), $"Error sending notification to user {user.Id}", ex);
            }
        }

        private async Task<NewsArticleDto?> GetRandomArticleForUser(string userId)
        {
            var preferredCategories = await _userService.GetUserPreferredCategoriesAsync(userId);
            var articles = await _newsService.GetArticlesByCategoriesAsync(preferredCategories);
            return articles.OrderBy(x => Guid.NewGuid()).FirstOrDefault();
        }

        private NotificationDto CreateNotificationDto(string userId, NewsArticleDto article)
        {
            return new NotificationDto
            {
                ApplicationUserId = userId,
                ArticleTitle = article.Title ?? "No title available",
                ArticleUrl = article.Domain_Url ?? "No url available",
                Category = article.Topic ?? "No topic available",
                CreatedAt = DateTime.UtcNow,
                ArticleDescription = article.Description ?? "No excerpt available",
                ArticleId = article.Id ?? "No Id available"
            };
        }

        private async Task SaveNotificationAsync(Notification notification)
        {
            await _unitOfWork.Repository<Notification>().AddAsync(notification);
            await _unitOfWork.CompleteAsync();
        }

        private async Task SendEmailAsync(NotificationDto notificationDto)
        {
            var user = await _userManager.FindByIdAsync(notificationDto.ApplicationUserId);
            var email = user.Email;
            await _mailSettings.SendNotificationEmail(notificationDto, email);
        }
    }
}
