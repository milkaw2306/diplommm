using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Diploma.Services
{
    public class EmailService
    {
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;
        private readonly string _fromEmail;
        private readonly string _fromName;

        public EmailService(
            string? smtpServer = null,
            int? smtpPort = null,
            string? username = null,
            string? password = null,
            string? fromEmail = null,
            string? fromName = null)
        {
            _smtpServer = smtpServer ?? Environment.GetEnvironmentVariable("PHOTO_DRIVE_SMTP_HOST") ?? "smtp.gmail.com";
            _smtpPort = smtpPort ?? ReadIntEnvironment("PHOTO_DRIVE_SMTP_PORT", 587);
            _smtpUsername = username ?? Environment.GetEnvironmentVariable("PHOTO_DRIVE_SMTP_USERNAME") ?? string.Empty;
            _smtpPassword = password ?? Environment.GetEnvironmentVariable("PHOTO_DRIVE_SMTP_PASSWORD") ?? string.Empty;
            _fromEmail = fromEmail ?? Environment.GetEnvironmentVariable("PHOTO_DRIVE_SMTP_FROM_EMAIL") ?? _smtpUsername;
            _fromName = fromName ?? Environment.GetEnvironmentVariable("PHOTO_DRIVE_SMTP_FROM_NAME") ?? "Photo Drive";
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            if (string.IsNullOrWhiteSpace(_smtpUsername) ||
                string.IsNullOrWhiteSpace(_smtpPassword) ||
                string.IsNullOrWhiteSpace(_fromEmail))
            {
                throw new InvalidOperationException(
                    "SMTP не настроен. Укажите PHOTO_DRIVE_SMTP_USERNAME, PHOTO_DRIVE_SMTP_PASSWORD и при необходимости PHOTO_DRIVE_SMTP_HOST/PHOTO_DRIVE_SMTP_PORT.");
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_fromName, _fromEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            message.Body = new BodyBuilder
            {
                HtmlBody = htmlBody,
                TextBody = StripHtml(htmlBody)
            }.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(_smtpServer, _smtpPort, GetSecureSocketOptions(_smtpPort));
            await client.AuthenticateAsync(_smtpUsername, _smtpPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }

        private static int ReadIntEnvironment(string name, int fallback)
        {
            return int.TryParse(Environment.GetEnvironmentVariable(name), out int value)
                ? value
                : fallback;
        }

        private static string StripHtml(string html)
        {
            return System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", string.Empty);
        }

        private static SecureSocketOptions GetSecureSocketOptions(int port)
        {
            return port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
        }
    }
}
