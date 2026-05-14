using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Diploma.Models;
using Diploma.Services;

namespace Diploma.ViewModels
{
    public partial class ShareViewModel : ObservableObject
    {
        private readonly FileService _fileService = new(App.ConnectionString);
        private readonly EmailService _emailService = new();

        [ObservableProperty]
        private string _shareCode = string.Empty;

        [ObservableProperty]
        private string _sharedItemName = string.Empty;

        [ObservableProperty]
        private bool _hasExpiry = true;

        [ObservableProperty]
        private bool _isLinkCreated;

        [ObservableProperty]
        private bool _isCreating;

        [ObservableProperty]
        private string _statusText = "Готов";

        [ObservableProperty]
        private string _recipientEmail = string.Empty;

        [ObservableProperty]
        private bool _hasError;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _isFolderShare;

        [ObservableProperty]
        private bool _isPhotoShare;

        public DateTime? ExpiryDate => HasExpiry ? DateTime.Now.AddDays(7) : null;

        private int? _folderId;
        private int? _photoId;

        public void InitializeWithFolder(Folder folder)
        {
            _folderId = folder.FolderId;
            _photoId = null;
            IsFolderShare = true;
            IsPhotoShare = false;
            SharedItemName = folder.FolderName ?? "Папка";
        }

        public void InitializeWithPhoto(Photo photo)
        {
            _photoId = photo.PhotoId;
            _folderId = null;
            IsPhotoShare = true;
            IsFolderShare = false;
            SharedItemName = photo.OriginalName ?? "Фото";
        }

        [RelayCommand]
        private async Task CreateShareLinkAsync()
        {
            try
            {
                ClearError();
                IsCreating = true;
                ShareCode = await _fileService.CreateShareLinkAsync(_folderId, _photoId, App.CurrentUserId, ExpiryDate);
                Clipboard.SetText(ShareCode);
                IsLinkCreated = true;
                StatusText = "Код доступа создан и скопирован";
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                IsCreating = false;
            }
        }

        [RelayCommand]
        private void CopyLink()
        {
            if (string.IsNullOrWhiteSpace(ShareCode)) return;

            Clipboard.SetText(ShareCode);
            StatusText = "Скопировано";
        }

        [RelayCommand]
        private async Task ShareViaEmailAsync()
        {
            if (string.IsNullOrWhiteSpace(ShareCode))
            {
                ShowError("Сначала создайте код доступа.");
                return;
            }

            if (string.IsNullOrWhiteSpace(RecipientEmail))
            {
                ShowError("Введите email получателя.");
                return;
            }

            try
            {
                ClearError();
                IsCreating = true;
                string body = $"""
                    <h2>Вам открыт доступ к фото</h2>
                    <p>Объект: <strong>{System.Net.WebUtility.HtmlEncode(SharedItemName)}</strong></p>
                    <p>Код доступа: <strong>{ShareCode}</strong></p>
                    <p>Срок действия: {(ExpiryDate?.ToString("dd.MM.yyyy HH:mm") ?? "без ограничения")}</p>
                    """;

                await _emailService.SendEmailAsync(RecipientEmail, "Доступ к фото", body);
                StatusText = "Письмо отправлено";
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                IsCreating = false;
            }
        }

        [RelayCommand]
        private void CloseWindow()
        {
            foreach (Window w in Application.Current.Windows)
            {
                if (w is Views.ShareDialog)
                    w.Close();
            }
        }

        partial void OnHasExpiryChanged(bool value)
        {
            OnPropertyChanged(nameof(ExpiryDate));
        }

        private void ShowError(string message)
        {
            HasError = true;
            ErrorMessage = message;
            StatusText = "Ошибка";
        }

        private void ClearError()
        {
            HasError = false;
            ErrorMessage = string.Empty;
        }
    }
}
