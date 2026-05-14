using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace Diploma.Services
{
    public class EmailService
    {
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;

        public EmailService(
            string smtpServer = "smtp.yandex.ru",
            int smtpPort = 587,
            string username = "milaperevozchikowa@yandex.ru",
            string password = "hwexrrmwzwturtwa")
        {
            _smtpServer = smtpServer;
            _smtpPort = smtpPort;
            _smtpUsername = username;
            _smtpPassword = password;
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            try
            {
                using (System.Net.Mail.SmtpClient client = new System.Net.Mail.SmtpClient(_smtpServer, _smtpPort))
                {
                    client.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);
                    client.EnableSsl = true;

                    MailMessage message = new MailMessage
                    {
                        From = new MailAddress(_smtpUsername, "Diplom_zxc"),
                        Subject = subject,
                        Body = htmlBody,
                        IsBodyHtml = true
                    };
                    message.To.Add(toEmail);

                    await client.SendMailAsync(message);
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}