using System;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Dapper;
using Diploma.Models;

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

            await _emailService.SendEmailAsync(email, "Сброс пароля", $"Код: {resetCode}");
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