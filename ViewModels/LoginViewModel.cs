using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Diploma.Services;

namespace Diploma.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly AuthService _authService;

        [ObservableProperty]
        private string _username = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private bool _isLoggingIn;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _hasError;

        public LoginViewModel()
        {
            var emailService = new EmailService();
            _authService = new AuthService(App.ConnectionString, emailService);
        }

        [RelayCommand]
        private async Task LoginAsync()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Заполните все поля";
                HasError = true;
                return;
            }

            try
            {
                IsLoggingIn = true;
                HasError = false;

                var user = await _authService.LoginAsync(Username, Password);

                App.CurrentUserId = user.UserId;
                App.CurrentUsername = user.Username;

                var mainWindow = new Views.MainWindow();
                mainWindow.Show();

                CloseCurrentWindow();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                HasError = true;
            }
            finally
            {
                IsLoggingIn = false;
            }
        }

        [RelayCommand]
        private void GoToRegister()
        {
            var registerWindow = new Views.RegisterWindow();
            registerWindow.Show();
            CloseCurrentWindow();
        }

        [RelayCommand]
        private void ForgotPassword()
        {
            var forgotPasswordWindow = new Views.ForgotPasswordWindow();
            forgotPasswordWindow.Show();
            CloseCurrentWindow();
        }

        private void CloseCurrentWindow()
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window is LoginWindow)
                {
                    window.Close();
                    break;
                }
            }
        }
    }
}