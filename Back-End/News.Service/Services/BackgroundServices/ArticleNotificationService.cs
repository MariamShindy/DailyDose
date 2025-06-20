namespace News.Service.Services.BackgroundServices
{
    public class ArticleNotificationService : IHostedService, IDisposable
    {
        private readonly ILoggingService _logger;
        private readonly IServiceProvider _serviceProvider;
        private Timer _timer;

        public ArticleNotificationService(ILoggingService logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInfo(nameof(ArticleNotificationService),"ArticleNotificationService starting...");
            _timer = new Timer(ExecuteTask, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));
            return Task.CompletedTask;
        }

        private async void ExecuteTask(object state)
        {
            _logger.LogInfo(nameof(ArticleNotificationService),$"Executing task at: {DateTime.UtcNow}");
            using (var scope = _serviceProvider.CreateScope())
            {
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                await notificationService.SendNotificationsAsync();
                if (scope.ServiceProvider is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync();
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
