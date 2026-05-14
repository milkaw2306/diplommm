using System.Windows;
using Diploma.ViewModels;

namespace Diploma.Views
{
    /// <summary>
    /// Логика взаимодействия для RegisterWindow.xaml
    /// </summary>
    public partial class RegisterWindow : Window
    {
        private readonly RegisterViewModel? _viewModel;

        public RegisterWindow()
        {
            InitializeComponent();
            _viewModel = DataContext as RegisterViewModel;

            // Привязываем PasswordBox к ViewModel
            PasswordBox.PasswordChanged += (s, e) =>
            {
                if (_viewModel != null)
                {
                    _viewModel.Password = PasswordBox.Password;
                }
            };

            // Привязываем ConfirmPasswordBox к ViewModel
            ConfirmPasswordBox.PasswordChanged += (s, e) =>
            {
                if (_viewModel != null)
                {
                    _viewModel.ConfirmPassword = ConfirmPasswordBox.Password;
                }
            };
        }
    }
}