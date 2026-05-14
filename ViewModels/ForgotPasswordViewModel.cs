using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Diploma.Services;

namespace Diploma.ViewModels
{
    public partial class ForgotPasswordViewModel : ObservableObject
    {
        private readonly AuthService _authService;

        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private string _resetCode = string.Empty;

        [ObservableProperty]
        private string _newPassword = string.Empty;

        [ObservableProperty]
        private bool _isCodeSent;

        [ObservableProperty]
        private bool _isSendingCode;

        [ObservableProperty]
        private bool _isResettingPassword;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _hasError;

        public ForgotPasswordViewModel()
        {
            var emailService = new EmailService();
            _authService = new AuthService(App.ConnectionString, emailService);
        }

        [RelayCommand]
        private async Task SendCodeAsync()
        {
            if (string.IsNullOrWhiteSpace(Email))
            {
                ErrorMessage = "Введите email";
                HasError = true;
                return;
            }

            try
            {
                IsSendingCode = true;
                HasError = false;

                await _authService.SendResetCodeAsync(Email);

                IsCodeSent = true;
                ErrorMessage = string.Empty;
                HasError = false;
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                HasError = true;
            }
            finally
            {
                IsSendingCode = false;
            }
        }

        [RelayCommand]
        private async Task ResetPasswordAsync()
        {
            if (string.IsNullOrWhiteSpace(ResetCode) || string.IsNullOrWhiteSpace(NewPassword))
            {
                ErrorMessage = "Заполните все поля";
                HasError = true;
                return;
            }

            if (ResetCode.Length != 6 || !int.TryParse(ResetCode, out _))
            {
                ErrorMessage = "Код должен состоять из 6 цифр";
                HasError = true;
                return;
            }

            try
            {
                IsResettingPassword = true;
                HasError = false;

                await _authService.ResetPasswordAsync(Email, ResetCode, NewPassword);

                System.Windows.MessageBox.Show("Пароль успешно изменен!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                GoToLogin();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                HasError = true;
            }
            finally
            {
                IsResettingPassword = false;
            }
        }

        [RelayCommand]
        private void GoToLogin()
        {
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            CloseCurrentWindow();
        }

        private void CloseCurrentWindow()
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window is Views.ForgotPasswordWindow)
                {
                    window.Close();
                    break;
                }
            }
        }
    }
}
