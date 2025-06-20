namespace News.Service.Services.BackgroundServices
{
    public class AccountDeletionService(ILoggingService _logger , IServiceScopeFactory _scopeFactory) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await DeleteExpiredAccountsAsync();
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }

        private async Task DeleteExpiredAccountsAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var expiredUsers = userManager.Users
                .Where(u => u.IsPendingDeletion && u.DeletionRequestedAt.HasValue)
                .AsEnumerable() 
                .Where(u => (DateTime.UtcNow - u.DeletionRequestedAt.Value).TotalDays >= 14)
                .ToList();

            foreach (var user in expiredUsers)
            {
                var result = await userManager.DeleteAsync(user);
                if (result.Succeeded)
                    _logger.LogInfo(nameof(AccountDeletionService),$"Deleted user {user.UserName} after 14 days.");
                else
                    _logger.LogError(nameof(AccountDeletionService),$"Failed to delete user {user.UserName}. Errors: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }

    }
}
