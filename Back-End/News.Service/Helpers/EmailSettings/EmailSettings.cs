namespace News.Service.Helpers.EmailSettings
{
    public class EmailSettings(IOptions<MailSettings> _options, IConfiguration _configuration, UserManager<ApplicationUser> _userManager) : IMailSettings
    {
        private readonly string frontBaseUrl = _configuration["FrontBaseUrl"]!;
        public async Task SendEmail(Email email)
        {
            var mail = new MimeMessage
            {
                Sender = MailboxAddress.Parse(_options.Value.Email),
                Subject = email.Subject
            };
            mail.To.Add(MailboxAddress.Parse(email.To));
            mail.From.Add(new MailboxAddress(_options.Value.DisplayName, _options.Value.Email));
            var builder = new BodyBuilder
            {
                HtmlBody = email.Body
            };
            mail.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            smtp.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
            smtp.CheckCertificateRevocation = false;

            try
            {
                await smtp.ConnectAsync(_options.Value.Host, _options.Value.Port, SecureSocketOptions.SslOnConnect);
                await smtp.AuthenticateAsync(_options.Value.Email, _options.Value.Password);
                await smtp.SendAsync(mail);
                await smtp.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to send email: {ex.Message}", ex);
            }
        }
        public async Task SendNotificationEmail(NotificationDto notificationDto, string userEmail)
        {
            var email = new Email
            {
                To = userEmail,
                Subject = $"New Article in {notificationDto.Category}",
                Body = BuildNotificationEmailBody(notificationDto, userEmail)
            };
            await SendEmail(email);
        }
        private string BuildNotificationEmailBody(NotificationDto notificationDto, string userEmail)
        {
            var user = _userManager.FindByIdAsync(notificationDto.ApplicationUserId).Result;
            var body = $@"
         <html>
         <head>
             <style>
                 body {{
                     font-family: 'Segoe UI', Arial, sans-serif;
                     background-color: #f4f4f4;
                     margin: 0;
                     padding: 0;
                     color: #333;
                 }}
                 .email-wrapper {{
                     width: 100%;
                     padding: 30px 0;
                     background-color: #f4f4f4;
                 }}
                 .email-container {{
                     max-width: 600px;
                     margin: 0 auto;
                     background-color: #ffffff;
                     border-radius: 8px;
                     overflow: hidden;
                     box-shadow: 0 0 10px rgba(0, 0, 0, 0.05);
                 }}
                 .email-header {{
                     background-color: #0066cc;
                     color: white;
                     text-align: center;
                     padding: 20px;
                 }}
                 .email-header h1 {{
                     margin: 0;
                     font-size: 24px;
                 }}
                 .email-body {{
                     padding: 30px;
                 }}
                 .email-body p {{
                     margin: 0 0 15px;
                 }}
                 .email-body .title {{
                     font-size: 18px;
                     font-weight: bold;
                     color: #0066cc;
                     margin: 10px 0;
                 }}
                 .cta-buttons {{
                     margin-top: 20px;
                 }}
                 .cta-buttons a {{
                     display: inline-block;
                     background-color: #0066cc;
                     color: #ffffff;
                     text-decoration: none;
                     padding: 12px 20px;
                     border-radius: 6px;
                     margin-right: 10px;
                     font-weight: bold;
                     transition: background-color 0.3s ease;
                 }}
                 .cta-buttons a:hover {{
                     background-color: #0056b3;
                 }}
                 .email-footer {{
                     background-color: #f0f0f0;
                     text-align: center;
                     font-size: 13px;
                     color: #777;
                     padding: 20px;
                 }}
             </style>
         </head>
         <body>
             <div class='email-wrapper'>
                 <div class='email-container'>
                     <div class='email-header'>
                         <h1>📰 New Article in {notificationDto.Category}</h1>
                     </div>
                     <div class='email-body'>
                         <p>Hi {user?.FirstName},</p>
                         <p>We thought you'd like this new article in your preferred category:</p>
                         <p class='title'>{notificationDto.ArticleTitle}</p>
                        <div class='cta-buttons'>
                        <a href='{frontBaseUrl}/{notificationDto.ArticleId}'>View on Our Site</a>
                        </div>
                        <p style='font-size: 13px; color: #888888; margin-top: 10px;'>
                            Or view it on the <a href='{notificationDto.ArticleUrl}' style='color: #007BFF;'>original source</a>.
                        </p>
                     </div>
                     <div class='email-footer'>
                         <p>This message was sent based on your notification preferences.</p>
                         <p>Thank you for staying informed with us!</p>
                     </div>
                 </div>
             </div>
         </body>
         </html>";
            return body;
        }

    }
}