using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Diploma.Services;
using Microsoft.Win32;

namespace Diploma.ViewModels
{
    public partial class ExportViewModel : ObservableObject
    {
        private readonly ExportService? _exportService;
        private readonly List<int> _selectedPhotoIds;
        private readonly int? _folderId;
        private readonly string? _folderName;

        [ObservableProperty]
        private string _exportPath = string.Empty;

        [ObservableProperty]
        private bool _isExporting;

        [ObservableProperty]
        private string _statusText = "Готов к экспорту";

        [ObservableProperty]
        private string _exportInfo = string.Empty;

        [ObservableProperty]
        private int _exportProgress;

        // Конструктор по умолчанию для дизайнера XAML
        public ExportViewModel()
        {
            _selectedPhotoIds = new List<int>();
            _folderId = null;
            _folderName = null;
            _exportService = null;
            ExportInfo = "Выберите папку для экспорта";
        }

        // Конструктор для экспорта выбранных фото
        public ExportViewModel(List<int> selectedPhotoIds, string connectionString)
        {
            _selectedPhotoIds = selectedPhotoIds;
            _exportService = new ExportService(connectionString);
            _folderId = null;
            ExportInfo = $"Выбрано фото: {selectedPhotoIds.Count} шт.";
        }

        // Конструктор для экспорта целой папки
        public ExportViewModel(int folderId, string folderName, string connectionString)
        {
            _folderId = folderId;
            _folderName = folderName;
            _selectedPhotoIds = new List<int>();
            _exportService = new ExportService(connectionString);
            ExportInfo = $"Экспорт папки: {folderName}";
        }

        [RelayCommand]
        private void BrowseFolder()
        {
            var dialog = new OpenFolderDialog();
            if (dialog.ShowDialog() == true)
            {
                ExportPath = dialog.FolderName;
            }
        }

        [RelayCommand]
        private async Task Export()
        {
            if (string.IsNullOrEmpty(ExportPath))
            {
                MessageBox.Show("Выберите папку для экспорта!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_exportService == null)
            {
                MessageBox.Show("Ошибка инициализации сервиса экспорта!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_selectedPhotoIds.Count == 0 && _folderId == null)
            {
                MessageBox.Show("Нет фото для экспорта!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsExporting = true;
            StatusText = "Экспорт...";
            ExportProgress = 0;

            bool result = false;

            try
            {
                if (_folderId.HasValue)
                {
                    result = await _exportService.ExportFolder(_folderId.Value, ExportPath);
                    StatusText = result ? "Папка экспортирована!" : "Ошибка при экспорте папки";
                }
                else if (_selectedPhotoIds.Any())
                {
                    result = await _exportService.ExportPhotos(_selectedPhotoIds, ExportPath);
                    StatusText = result ? "Фото экспортированы!" : "Ошибка при экспорте фото";
                }
            }
            catch (Exception ex)
            {
                StatusText = $"Ошибка: {ex.Message}";
                MessageBox.Show($"Ошибка при экспорте: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsExporting = false;

                if (result)
                {
                    MessageBox.Show($"Экспорт завершен!\nПапка: {ExportPath}", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
    }
}