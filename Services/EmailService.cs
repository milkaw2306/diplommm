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

        // Обычная отправка письма
        public async Task<bool> SendEmailAsync(
            string toEmail,
            string subject,
            string htmlBody)
        {
            try
            {
                using (var client = new SmtpClient(_smtpServer, _smtpPort))
                {
                    client.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);
                    client.EnableSsl = true;
                    client.Timeout = 10000; // 10 секунд таймаут

                    var message = new MailMessage
                    {
                        From = new MailAddress(_smtpUsername, "Diploma System"),
                        Subject = subject,
                        Body = htmlBody,
                        IsBodyHtml = true
                    };

                    message.To.Add(toEmail);

                    await client.SendMailAsync(message);
                }

                return true;
            }
            catch (Exception ex)
            {
                // Лучше использовать нормальное логирование вместо Console.WriteLine
                Console.WriteLine($"[ERROR] Failed to send email: {ex.Message}");
                return false;
            }
        }

        // Проверка срока хранения файла с несколькими порогами
        public async Task CheckExpirationAndSendAsync(
            string toEmail,
            string fileName,
            DateTime expirationDate)
        {
            // Сколько дней осталось (без учета времени)
            int daysLeft = (expirationDate.Date - DateTime.Now.Date).Days;

            // Не отправляем, если срок уже истек или файл удален
            if (daysLeft < 0)
                return;

            string subject = null;
            string body = null;

            // Разные уведомления в зависимости от срока
            if (daysLeft == 14)
            {
                subject = "Срок хранения файла истекает через 2 недели";
                body = $@"
                    <h2>Уведомление о сроке хранения</h2>
                    <p>Файл <b>{fileName}</b> будет удалён через <b style='color: orange;'>14 дней</b>.</p>
                    <p>Дата удаления: <b>{expirationDate:dd.MM.yyyy}</b></p>
                    <hr />
                    <small>Это предварительное уведомление.</small>";
            }
            else if (daysLeft == 7)
            {
                subject = "Срок хранения файла заканчивается через неделю";
                body = $@"
                    <h2>Напоминание</h2>
                    <p>Файл <b>{fileName}</b> будет удалён через <b style='color: orange;'>7 дней</b>.</p>
                    <p>Дата удаления: <b>{expirationDate:dd.MM.yyyy}</b></p>
                    <p><i>Пожалуйста, сохраните копию файла, если он вам нужен.</i></p>";
            }
            else if (daysLeft == 3)
            {
                subject = "СРОЧНО! Файл будет удалён через 3 дня";
                body = $@"
                    <h2 style='color: red;'>Важное уведомление!</h2>
                    <p>Файл <b>{fileName}</b> будет удалён через <b style='color: red;'>3 дня</b>.</p>
                    <p>Дата удаления: <b>{expirationDate:dd.MM.yyyy}</b></p>
                    <p><b>Пожалуйста, срочно сохраните файл, если он вам нужен!</b></p>";
            }
            else if (daysLeft == 1)
            {
                subject = "ПОСЛЕДНЕЕ ПРЕДУПРЕЖДЕНИЕ! Файл будет удалён ЗАВТРА";
                body = $@"
                    <h2 style='color: red;'>ВНИМАНИЕ!</h2>
                    <p>Файл <b>{fileName}</b> будет удалён <b>ЗАВТРА</b>!</p>
                    <p>Дата удаления: <b>{expirationDate:dd.MM.yyyy}</b></p>
                    <p><b style='color: red;'>Немедленно сохраните файл, иначе он будет безвозвратно утерян!</b></p>";
            }

            // Отправляем только если есть уведомление
            if (subject != null && body != null)
            {
                await SendEmailAsync(toEmail, subject, body);
            }
        }

        // Проверка и отправка для нескольких порогов за один вызов
        public async Task SendExpirationReminderAsync(
            string toEmail,
            string fileName,
            DateTime expirationDate,
            params int[] reminderDays)
        {
            if (reminderDays.Length == 0)
                reminderDays = new[] { 14, 7, 3, 1 }; // По умолчанию

            int daysLeft = (expirationDate.Date - DateTime.Now.Date).Days;

            foreach (var day in reminderDays)
            {
                if (daysLeft == day)
                {
                    await CheckExpirationAndSendAsync(toEmail, fileName, expirationDate);
                    break; // Отправляем только одно уведомление в день
                }
            }
        }
    }
}