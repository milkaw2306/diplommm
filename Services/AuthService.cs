using Dapper;
using Diploma.Models;
using Diploma.Services;
using MySql.Data.MySqlClient;
using System;
using System.Threading.Tasks;

namespace Diploma.Services
{
    public class AuthService
    {
        private readonly string _connectionString;
        private readonly EmailService _emailService;

        public AuthService(string connectionString, EmailService emailService)
        {
            _connectionString = connectionString;
            _emailService = emailService;
        }

        public async Task<bool> RegisterUserAsync(string username, string email, string password, string fullName)
        {
            using var connection = new MySqlConnection(_connectionString);

            var existingUser = await connection.QueryFirstOrDefaultAsync<User>(
                "SELECT UserId FROM Users WHERE Username = @Username OR Email = @Email",
                new { Username = username, Email = email });

            if (existingUser != null)
                throw new Exception("Пользователь с таким именем или email уже существует");

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

            await connection.ExecuteAsync(
                "INSERT INTO Users (Username, Email, PasswordHash, FullName) VALUES (@Username, @Email, @PasswordHash, @FullName)",
                new { Username = username, Email = email, PasswordHash = passwordHash, FullName = fullName });

            return true;
        }

        public async Task<User> LoginAsync(string username, string password)
        {
            using var connection = new MySqlConnection(_connectionString);

            var user = await connection.QueryFirstOrDefaultAsync<User>(
                "SELECT * FROM Users WHERE Username = @Username AND IsActive = TRUE",
                new { Username = username });

            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                throw new Exception("Неверное имя пользователя или пароль");

            await connection.ExecuteAsync(
                "UPDATE Users SET LastLogin = NOW() WHERE UserId = @UserId",
                new { user.UserId });

            return user;
        }

        public async Task SendResetCodeAsync(string email)
        {
            using var connection = new MySqlConnection(_connectionString);

            var user = await connection.QueryFirstOrDefaultAsync<User>(
                "SELECT * FROM Users WHERE Email = @Email", new { Email = email });

            if (user == null)
                throw new Exception("Пользователь с таким email не найден");

            string resetCode = new Random().Next(100000, 999999).ToString();

            await connection.ExecuteAsync(
                "UPDATE Users SET ResetCode = @ResetCode, ResetCodeExpiry = DATE_ADD(NOW(), INTERVAL 15 MINUTE) WHERE UserId = @UserId",
                new { ResetCode = resetCode, UserId = user.UserId });

            string subject = "Diplom_zxc - Восстановление пароля";
            string body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <div style='max-width: 500px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #673AB7;'>Восстановление пароля</h2>
                        <p>Здравствуйте, <b>{user.Username}</b>!</p>
                        <p>Вы запросили восстановление пароля в приложении <b>Diplom_zxc</b>.</p>
                        <div style='background: #f5f5f5; padding: 20px; border-radius: 10px; text-align: center; margin: 20px 0;'>
                            <p style='font-size: 14px; color: #666;'>Ваш код для сброса пароля:</p>
                            <p style='font-size: 32px; font-weight: bold; color: #673AB7; letter-spacing: 10px;'>{resetCode}</p>
                        </div>
                        <p style='color: #999; font-size: 12px;'>Код действителен в течение <b>15 минут</b>.</p>
                        <p style='color: #999; font-size: 12px;'>Если вы не запрашивали сброс пароля, просто проигнорируйте это письмо.</p>
                        <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;'/>
                        <p style='color: #999; font-size: 11px;'>С уважением, команда Diplom_zxc</p>
                    </div>
                </body>
                </html>";

            bool sent = await _emailService.SendEmailAsync(email, subject, body);

            if (!sent)
                throw new Exception("Не удалось отправить письмо. Проверьте настройки SMTP.");
        }

        public async Task<bool> ResetPasswordAsync(string email, string code, string newPassword)
        {
            using var connection = new MySqlConnection(_connectionString);

            var user = await connection.QueryFirstOrDefaultAsync<User>(
                "SELECT * FROM Users WHERE Email = @Email AND ResetCode = @ResetCode AND ResetCodeExpiry > NOW()",
                new { Email = email, ResetCode = code });

            if (user == null)
                throw new Exception("Неверный или истекший код сброса");

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

            await connection.ExecuteAsync(
                "UPDATE Users SET PasswordHash = @PasswordHash, ResetCode = NULL, ResetCodeExpiry = NULL WHERE UserId = @UserId",
                new { PasswordHash = passwordHash, UserId = user.UserId });

            return true;
        }
    }
}