namespace News.Core.Contracts.Interfaces
{
    public interface IUserService
    {
        Task<List<UserPreferencesDto>> GetUsersPreferencesAsync();
        Task<ApplicationUser> GetCurrentUserAsync();
        Task<bool> SendFeedbackAsync(FeedbackDto feedbackDto);
        Task<bool> SendSurveyAsync(SurveyDto surveyDto);
        Task<List<UserDto>> GetAllUsersAsync();
        Task<IdentityResult> UpdateUserAsync(EditUserDto editUserDto);
        Task SetUserPreferredCategoriesAsync(ApplicationUser user, ICollection<string> categoryNames);
        Task<IEnumerable<CategoryDto>> GetUserPreferredCategoriesAsync();
        Task<IEnumerable<CategoryDto>> GetUserPreferredCategoriesAsync(string userId);
        Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(string userId);
        Task<IEnumerable<SurveyResponseDto>> GetAllSurvyesAsync();
        Task<IdentityResult> RequestAccountDeletionAsync(string userId);

    }
}
