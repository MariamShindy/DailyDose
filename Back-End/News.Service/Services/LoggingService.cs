
namespace News.Service.Services
{
    public class LoggingService<T>(ILogger<T> _logger) : ILoggingService
    {
        public void LogError(string service, string message, Exception? ex = null)
        {
            _logger.LogError(ex, $"[{service}] --> {message}");
        }

        public void LogInfo(string service, string message)
        {
            _logger.LogInformation($"[{service}] --> {message}");
        }

        public void LogWarning(string service, string message)
        {
            _logger.LogWarning($"[{service}] --> {message}");
        }
    }
}
