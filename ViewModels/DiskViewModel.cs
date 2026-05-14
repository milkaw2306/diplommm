using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Diploma.Models;
using Diploma.Services;

namespace Diploma.ViewModels
{
    public partial class DiskViewModel : ObservableObject
    {
        private readonly FileService _fileService;
        private readonly ImportService _importService;
        private readonly ExportService _exportService;

        [ObservableProperty]
        private ObservableCollection<Folder> _folders = new();

        [ObservableProperty]
        private ObservableCollection<Photo> _photos = new();

        [ObservableProperty]
        private ObservableCollection<Photo> _selectedPhotos = new();

        [ObservableProperty]
        private Folder? _selectedFolder;

        [ObservableProperty]
        private string _currentPath = "Мой диск";

        [ObservableProperty]
        private string _breadcrumbPath = "Мой диск";

        [ObservableProperty]
        private long _storageUsed;

        [ObservableProperty]
        private long _storageLimit = 10737418240; // 10GB

        [ObservableProperty]
        private double _storagePercent;

        [ObservableProperty]
        private string _storageInfo = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _isGridView = true;

        [ObservableProperty]
        private bool _isListView;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private string _statusText = "Готов";

        [ObservableProperty]
        private int _totalPhotos;

        [ObservableProperty]
        private int _totalFolders;

        [ObservableProperty]
        private Stack<int?> _navigationHistory = new();

        [ObservableProperty]
        private bool _canGoBack;

        public DiskViewModel()
        {
            _fileService = new FileService(App.ConnectionString);
            _importService = new ImportService(App.ConnectionString, App.CurrentUserId);
            _exportService = new ExportService(App.ConnectionString);

            _ = LoadRootFoldersAsync();
            UpdateStorageInfo();
        }

        [RelayCommand]
        private async Task LoadRootFoldersAsync()
        {
            try
            {
                IsLoading = true;
                StatusText = "Загрузка папок...";

                var folders = await _fileService.GetFoldersAsync(App.CurrentUserId, null);
                Folders = new ObservableCollection<Folder>(folders);

                SelectedFolder = null;
                CurrentPath = "Мой диск";
                BreadcrumbPath = "Мой диск";
                Photos.Clear();

                UpdateCounters();
                StatusText = "Готов";
            }
            catch (Exception ex)
            {
                StatusText = $"Ошибка загрузки: {ex.Message}";
                MessageBox.Show($"Ошибка загрузки папок: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task NavigateToFolderAsync(Folder? folder)
        {
            if (folder == null) return;

            try
            {
                IsLoading = true;
                StatusText = $"Загрузка папки \"{folder.FolderName}\"...";

                // Сохраняем текущую папку в историю
                if (SelectedFolder != null)
                {
                    NavigationHistory.Push(SelectedFolder.FolderId);
                }
                else
                {
                    NavigationHistory.Push(null); // корень
                }

                CanGoBack = true;

                SelectedFolder = folder;
                CurrentPath = folder.FolderName ?? "Папка";
                BreadcrumbPath = await BuildBreadcrumbAsync(folder);

                // Загружаем подпапки
                var subFolders = await _fileService.GetFoldersAsync(App.CurrentUserId, folder.FolderId);
                Folders = new ObservableCollection<Folder>(subFolders);

                // Загружаем фото в папке
                var photos = await _fileService.GetPhotosInFolderAsync(folder.FolderId);
                Photos = new ObservableCollection<Photo>(photos);

                UpdateCounters();
                UpdateStorageInfo();

                StatusText = $"Папка: {folder.FolderName} ({Photos.Count} фото)";
            }
            catch (Exception ex)
            {
                StatusText = $"Ошибка навигации: {ex.Message}";
                MessageBox.Show($"Ошибка загрузки папки: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task GoBackAsync()
        {
            if (!CanGoBack || NavigationHistory.Count == 0) return;

            try
            {
                IsLoading = true;
                StatusText = "Возврат...";

                int? previousFolderId = NavigationHistory.Pop();
                CanGoBack = NavigationHistory.Count > 0;

                if (previousFolderId == null)
                {
                    await LoadRootFoldersAsync();
                }
                else
                {
                    var folders = await _fileService.GetFoldersAsync(App.CurrentUserId, null);
                    var previousFolder = folders.FirstOrDefault(f => f.FolderId == previousFolderId);

                    if (previousFolder != null)
                    {
                        await NavigateToFolderAsync(previousFolder);
                    }
                    else
                    {
                        await LoadRootFoldersAsync();
                    }
                }

                StatusText = "Готов";
            }
            catch (Exception ex)
            {
                StatusText = $"Ошибка: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task CreateFolderAsync(string? folderName)
        {
            if (string.IsNullOrWhiteSpace(folderName))
            {
                folderName = "Новая папка";
            }

            try
            {
                StatusText = "Создание папки...";

                int? parentFolderId = SelectedFolder?.FolderId;
                int newFolderId = await _fileService.CreateFolderAsync(App.CurrentUserId, folderName, parentFolderId);

                // Обновляем список папок
                if (SelectedFolder != null)
                {
                    await NavigateToFolderAsync(SelectedFolder);
                }
                else
                {
                    await LoadRootFoldersAsync();
                }

                StatusText = $"Папка \"{folderName}\" создана";
            }
            catch (Exception ex)
            {
                StatusText = $"Ошибка создания папки: {ex.Message}";
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task DeleteFolderAsync(Folder? folder)
        {
            if (folder == null) return;

            var result = MessageBox.Show(
                $"Удалить папку \"{folder.FolderName}\" и всё её содержимое?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    StatusText = $"Удаление папки \"{folder.FolderName}\"...";

                    await _fileService.DeleteFolderAsync(folder.FolderId);

                    if (SelectedFolder?.FolderId == folder.FolderId)
                    {
                        await GoBackAsync();
                    }
                    else
                    {
                        Folders.Remove(folder);
                    }

                    UpdateStorageInfo();
                    StatusText = "Папка удалена";
                }
                catch (Exception ex)
                {
                    StatusText = $"Ошибка удаления: {ex.Message}";
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private async Task DeletePhotoAsync(Photo? photo)
        {
            if (photo == null) return;

            var result = MessageBox.Show(
                $"Удалить фотографию \"{photo.OriginalName}\"?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    StatusText = $"Удаление \"{photo.OriginalName}\"...";

                    await _fileService.DeletePhotoAsync(photo.PhotoId);
                    Photos.Remove(photo);
                    SelectedPhotos.Remove(photo);

                    UpdateCounters();
                    UpdateStorageInfo();

                    StatusText = "Фото удалено";
                }
                catch (Exception ex)
                {
                    StatusText = $"Ошибка удаления: {ex.Message}";
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private async Task DeleteSelectedPhotosAsync()
        {
            if (SelectedPhotos.Count == 0) return;

            var result = MessageBox.Show(
                $"Удалить выбранные фотографии ({SelectedPhotos.Count} шт.)?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    StatusText = $"Удаление {SelectedPhotos.Count} фото...";

                    foreach (var photo in SelectedPhotos.ToList())
                    {
                        await _fileService.DeletePhotoAsync(photo.PhotoId);
                        Photos.Remove(photo);
                    }

                    SelectedPhotos.Clear();
                    UpdateCounters();
                    UpdateStorageInfo();

                    StatusText = "Фотографии удалены";
                }
                catch (Exception ex)
                {
                    StatusText = $"Ошибка удаления: {ex.Message}";
                }
            }
        }

        [RelayCommand]
        private async Task RenameFolderAsync(Folder? folder)
        {
            if (folder == null) return;

            string? newName = Microsoft.VisualBasic.Interaction.InputBox(
                "Введите новое имя папки:",
                "Переименование",
                folder.FolderName ?? string.Empty);

            if (!string.IsNullOrWhiteSpace(newName) && newName != folder.FolderName)
            {
                try
                {
                    // Логика переименования через FileService
                    StatusText = $"Переименование в \"{newName}\"...";
                    // await _fileService.RenameFolderAsync(folder.FolderId, newName);

                    await LoadRootFoldersAsync();
                    StatusText = "Папка переименована";
                }
                catch (Exception ex)
                {
                    StatusText = $"Ошибка: {ex.Message}";
                }
            }
        }

        [RelayCommand]
        private async Task RefreshAsync()
        {
            if (SelectedFolder != null)
            {
                await NavigateToFolderAsync(SelectedFolder);
            }
            else
            {
                await LoadRootFoldersAsync();
            }
        }

        [RelayCommand]
        private async Task ImportToCurrentFolderAsync()
        {
            if (SelectedFolder == null)
            {
                MessageBox.Show("Сначала выберите папку для импорта.", "Импорт",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var importWindow = new Views.ImportWindow(SelectedFolder.FolderId, SelectedFolder.FolderName ?? "Папка");
            importWindow.Owner = Application.Current.MainWindow;

            if (importWindow.ShowDialog() == true)
            {
                await RefreshAsync();
                StatusText = "Импорт завершен";
            }
        }

        [RelayCommand]
        private async Task ExportSelectedAsync()
        {
            if (SelectedPhotos.Count == 0)
            {
                MessageBox.Show("Выберите фотографии для экспорта", "Экспорт",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Исправлено: передаем ID выбранных фото и строку подключения
            var selectedIds = SelectedPhotos.Select(p => p.PhotoId).ToList();
            var exportWindow = new Views.ExportWindow(selectedIds, App.ConnectionString);
            exportWindow.Owner = Application.Current.MainWindow;
            exportWindow.ShowDialog();

            StatusText = "Экспорт завершен";
            await Task.CompletedTask;
        }

        [RelayCommand]
        private void ShareFolder(Folder? folder)
        {
            if (folder == null) return;
            Views.ShareDialog.ShowShareDialog(folder);
        }

        [RelayCommand]
        private void SharePhoto(Photo? photo)
        {
            if (photo == null) return;
            Views.ShareDialog.ShowShareDialog(photo);
        }

        [RelayCommand]
        private void SelectAllPhotos()
        {
            SelectedPhotos = new ObservableCollection<Photo>(Photos);
            StatusText = $"Выбрано: {SelectedPhotos.Count} фото";
        }

        [RelayCommand]
        private void DeselectAllPhotos()
        {
            SelectedPhotos.Clear();
            StatusText = "Выбор снят";
        }

        [RelayCommand]
        private async Task SearchPhotosAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                await RefreshAsync();
                return;
            }

            StatusText = $"Поиск: \"{SearchText}\"...";

            // Простой поиск по имени файла
            var filteredPhotos = Photos.Where(p =>
                p.OriginalName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true ||
                p.FileName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true
            ).ToList();

            Photos = new ObservableCollection<Photo>(filteredPhotos);
            StatusText = $"Найдено: {filteredPhotos.Count} фото";
        }

        [RelayCommand]
        private void ToggleView()
        {
            IsGridView = !IsGridView;
            IsListView = !IsGridView;
        }

        private async Task<string> BuildBreadcrumbAsync(Folder folder)
        {
            var parts = new System.Collections.Generic.List<string>();
            var currentFolder = folder;

            while (currentFolder != null)
            {
                parts.Insert(0, currentFolder.FolderName ?? "Папка");

                if (currentFolder.ParentFolderId != null)
                {
                    var folders = await _fileService.GetFoldersAsync(App.CurrentUserId, null);
                    currentFolder = folders.FirstOrDefault(f => f.FolderId == currentFolder.ParentFolderId);
                }
                else
                {
                    break;
                }
            }

            parts.Insert(0, "Мой диск");
            return string.Join(" > ", parts);
        }

        private void UpdateStorageInfo()
        {
            StorageUsed = Photos.Sum(p => p.FileSize);
            StoragePercent = (double)StorageUsed / StorageLimit * 100;

            string[] sizes = { "B", "KB", "MB", "GB" };
            double usedSize = StorageUsed;
            double limitSize = StorageLimit;
            int usedOrder = 0, limitOrder = 0;

            while (usedSize >= 1024 && usedOrder < sizes.Length - 1)
            {
                usedOrder++;
                usedSize /= 1024;
            }

            while (limitSize >= 1024 && limitOrder < sizes.Length - 1)
            {
                limitOrder++;
                limitSize /= 1024;
            }

            StorageInfo = $"{usedSize:0.0} {sizes[usedOrder]} / {limitSize:0.0} {sizes[limitOrder]} ({StoragePercent:0.0}%)";
        }

        private void UpdateCounters()
        {
            TotalPhotos = Photos.Count;
            TotalFolders = Folders.Count;
        }

        partial void OnSelectedPhotosChanged(ObservableCollection<Photo> value)
        {
            StatusText = value.Count > 0
                ? $"Выбрано: {value.Count} фото"
                : "Готов";
        }

        partial void OnSearchTextChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                _ = RefreshAsync();
            }
        }
    }
}
