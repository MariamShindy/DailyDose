namespace News.Core.Contracts.Interfaces
{
    public interface ILoggingService
    {
        void LogInfo(string service, string message);
        void LogWarning(string service, string message);
        void LogError(string service, string message, Exception? ex = null);
    }
}
