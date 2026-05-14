using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Diploma.Services;

namespace Diploma.ViewModels
{
    public partial class RegisterViewModel : ObservableObject
    {
        private readonly AuthService _authService;

        [ObservableProperty]
        private string _username = string.Empty;

        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private string _confirmPassword = string.Empty;

        [ObservableProperty]
        private string _fullName = string.Empty;

        [ObservableProperty]
        private bool _isRegistering;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _hasError;

        public RegisterViewModel()
        {
            var emailService = new EmailService();
            _authService = new AuthService(App.ConnectionString, emailService);
        }

        [RelayCommand]
        private async Task RegisterAsync()
        {
            if (!ValidateInputs()) return;

            try
            {
                IsRegistering = true;
                HasError = false;

                await _authService.RegisterUserAsync(Username, Email, Password, FullName);

                System.Windows.MessageBox.Show("Регистрация успешна! Теперь вы можете войти в систему.",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                GoToLogin();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                HasError = true;
            }
            finally
            {
                IsRegistering = false;
            }
        }

        [RelayCommand]
        private void GoToLogin()
        {
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            CloseCurrentWindow();
        }

        private bool ValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Email) ||
                string.IsNullOrWhiteSpace(Password) || string.IsNullOrWhiteSpace(ConfirmPassword))
            {
                ErrorMessage = "Заполните все обязательные поля";
                HasError = true;
                return false;
            }

            if (Username.Length < 3)
            {
                ErrorMessage = "Имя пользователя должно содержать минимум 3 символа";
                HasError = true;
                return false;
            }

            if (!Regex.IsMatch(Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                ErrorMessage = "Введите корректный email";
                HasError = true;
                return false;
            }

            if (Password.Length < 6)
            {
                ErrorMessage = "Пароль должен содержать минимум 6 символов";
                HasError = true;
                return false;
            }

            if (Password != ConfirmPassword)
            {
                ErrorMessage = "Пароли не совпадают";
                HasError = true;
                return false;
            }

            return true;
        }

        private void CloseCurrentWindow()
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window is Views.RegisterWindow)
                {
                    window.Close();
                    break;
                }
            }
        }
    }
}