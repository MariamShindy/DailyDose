namespace News.Core.Contracts.Interfaces
{
    public interface IMailSettings
    {
        Task SendEmail(Email email);
        Task SendNotificationEmail(NotificationDto notificationDto, string userEmail);
    }
}
