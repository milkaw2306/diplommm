using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Diploma.Services;
using Microsoft.Win32;
using System.Windows;

namespace Diploma.ViewModels
{
    public partial class ImportViewModel : ObservableObject
    {
        private readonly ImportService _importService;
        private readonly int _targetFolderId;

        [ObservableProperty]
        private string _statusText = "Готов к импорту";

        [ObservableProperty]
        private bool _isImporting;

        [ObservableProperty]
        private string _currentFile = string.Empty;

        [ObservableProperty]
        private int _progressValue;

        [ObservableProperty]
        private int _totalFiles;

        [ObservableProperty]
        private string _tags = string.Empty;

        [ObservableProperty]
        private string _targetFolderName = "Папка не выбрана";

        [ObservableProperty]
        private bool _hasError;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        public ImportViewModel() : this(0, "Папка не выбрана")
        {
        }

        public ImportViewModel(int targetFolderId, string targetFolderName)
        {
            _targetFolderId = targetFolderId;
            TargetFolderName = targetFolderName;
            _importService = new ImportService(App.ConnectionString, App.CurrentUserId);
            _importService.ProgressChanged += OnProgressChanged;
            _importService.ImportCompleted += OnImportCompleted;
        }

        private void OnProgressChanged(object? sender, ImportProgressEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                CurrentFile = e.CurrentFile ?? "";
                ProgressValue = e.ProcessedCount;
                TotalFiles = e.TotalCount;
                StatusText = $"Импортировано {e.ProcessedCount} из {e.TotalCount}";
            });
        }

        private void OnImportCompleted(object? sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                IsImporting = false;
                StatusText = "Импорт завершен";
            });
        }

        [RelayCommand]
        private async Task ImportFolderAsync()
        {
            if (!ValidateBeforeImport()) return;

            var dialog = new OpenFolderDialog();
            if (dialog.ShowDialog() == true)
            {
                await RunImportAsync(() => _importService.ImportFolderWithStructure(dialog.FolderName, _targetFolderId, Tags));
            }
        }

        [RelayCommand]
        private async Task ImportFilesAsync()
        {
            if (!ValidateBeforeImport()) return;

            var dialog = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp;*.gif"
            };

            if (dialog.ShowDialog() == true)
            {
                await RunImportAsync(() => _importService.ImportDraggedFiles(dialog.FileNames, _targetFolderId, Tags));
            }
        }

        private async Task RunImportAsync(Func<Task<int>> importAction)
        {
            try
            {
                ClearError();
                IsImporting = true;
                ProgressValue = 0;
                TotalFiles = 0;
                StatusText = "Импорт начинается...";

                int imported = await importAction();
                StatusText = $"Импорт завершен: {imported} фото";

                if (imported == 0)
                {
                    ShowError("Не удалось импортировать фотографии. Проверьте формат файлов и настройки базы данных.");
                }
            }
            catch (Exception ex)
            {
                IsImporting = false;
                ShowError(ex.Message);
            }
        }

        private bool ValidateBeforeImport()
        {
            if (_targetFolderId <= 0)
            {
                ShowError("Перед импортом выберите папку в главном окне.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(Tags))
            {
                ShowError("Добавьте теги через запятую, например: свадьба, портрет, лето.");
                return false;
            }

            ClearError();
            return true;
        }

        private void ShowError(string message)
        {
            HasError = true;
            ErrorMessage = message;
            StatusText = message;
        }

        private void ClearError()
        {
            HasError = false;
            ErrorMessage = string.Empty;
        }
    }
}
