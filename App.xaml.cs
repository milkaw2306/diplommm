using Diploma.Services;
using System.Configuration;
using System.Data;
using System.Windows;

namespace Diploma
{
    public partial class App : Application
    {
        public static int CurrentUserId { get; set; }
        public static string? CurrentUsername { get; set; }
        public static string ConnectionString { get; } =
            "server=localhost;port=3306;database=diploma;uid=root;password=;";

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                var dbService = new DatabaseService(ConnectionString);
                await dbService.InitializeDatabaseAsync();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Ошибка подключения к базе данных: {ex.Message}\n\n" +
                    "Проверьте, запущен ли MySQL сервер и правильно ли указаны настройки подключения.",
                    "Ошибка базы данных",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
