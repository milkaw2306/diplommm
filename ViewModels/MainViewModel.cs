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
    public partial class MainViewModel : ObservableObject
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
        private Photo? _selectedPhoto;

        [ObservableProperty]
        private string _currentPath = "Мой диск";

        [ObservableProperty]
        private string _breadcrumbPath = "Мой диск";

        [ObservableProperty]
        private Stack<int?> _navigationHistory = new();

        [ObservableProperty]
        private bool _canGoBack;

        [ObservableProperty]
        private long _storageUsed;

        [ObservableProperty]
        private long _storageLimit = 10737418240;

        [ObservableProperty]
        private double _storagePercent;

        [ObservableProperty]
        private string _storageDisplay = "0 B / 10 GB";

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
        private string _userName = App.CurrentUsername ?? "Пользователь";

        [ObservableProperty]
        private int _totalPhotos;

        [ObservableProperty]
        private int _totalFolders;

        public MainViewModel()
        {
            _fileService = new FileService(App.ConnectionString);
            _importService = new ImportService(App.ConnectionString, App.CurrentUserId);
            _exportService = new ExportService(App.ConnectionString);

            _ = LoadDataAsync();
        }

        // Все команды должны быть public для доступа из View
        [RelayCommand]
        public async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;
                var folders = await _fileService.GetFoldersAsync(App.CurrentUserId);
                Folders = new ObservableCollection<Folder>(folders);

                if (SelectedFolder != null)
                {
                    var photos = await _fileService.GetPhotosInFolderAsync(SelectedFolder.FolderId);
                    Photos = new ObservableCollection<Photo>(photos);
                }
                else
                {
                    Photos.Clear();
                }

                UpdateCounters();
                UpdateStorageInfo();
                StatusText = "Готов";
            }
            catch (Exception ex)
            {
                StatusText = "Ошибка: " + ex.Message;
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task NavigateToFolderAsync(Folder? folder)
        {
            if (folder == null) return;

            IsLoading = true;
            NavigationHistory.Push(SelectedFolder?.FolderId);
            CanGoBack = true;
            SelectedFolder = folder;

            var subFolders = await _fileService.GetFoldersAsync(App.CurrentUserId, folder.FolderId);
            Folders = new ObservableCollection<Folder>(subFolders);

            var photos = await _fileService.GetPhotosInFolderAsync(folder.FolderId);
            Photos = new ObservableCollection<Photo>(photos);

            CurrentPath = folder.FolderName ?? "Папка";
            BreadcrumbPath = $"Мой диск / {CurrentPath}";
            UpdateCounters();
            UpdateStorageInfo();
            StatusText = $"Папка: {CurrentPath}";
            IsLoading = false;
        }

        [RelayCommand]
        public async Task GoBackAsync()
        {
            if (!CanGoBack) return;

            int? previousId = NavigationHistory.Pop();
            CanGoBack = NavigationHistory.Count > 0;

            if (previousId == null)
            {
                SelectedFolder = null;
                CurrentPath = "Мой диск";
                BreadcrumbPath = "Мой диск";
                await LoadDataAsync();
            }
        }

        [RelayCommand]
        public async Task CreateFolderAsync()
        {
            string? folderName = "Новая папка";
            if (string.IsNullOrWhiteSpace(folderName)) return;

            int? parentId = SelectedFolder?.FolderId;
            await _fileService.CreateFolderAsync(App.CurrentUserId, folderName, parentId);
            await LoadDataAsync();
        }

        [RelayCommand]
        public async Task DeletePhotoAsync(Photo? photo)
        {
            if (photo == null) return;

            var result = MessageBox.Show("Удалить фото?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                await _fileService.DeletePhotoAsync(photo.PhotoId);
                Photos.Remove(photo);
            }
        }

        [RelayCommand]
        public async Task DeleteFolderAsync(Folder? folder)
        {
            if (folder == null) return;

            var result = MessageBox.Show("Удалить папку?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                await _fileService.DeleteFolderAsync(folder.FolderId);
                Folders.Remove(folder);
            }
        }

        [RelayCommand]
        public void OpenImportWindow()
        {
            if (SelectedFolder == null)
            {
                MessageBox.Show("Сначала выберите папку, куда нужно добавить фотографии.", "Импорт",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var importWindow = new Views.ImportWindow(SelectedFolder.FolderId, SelectedFolder.FolderName ?? "Папка");
            importWindow.Owner = Application.Current.MainWindow;
            importWindow.ShowDialog();
            _ = LoadDataAsync();
        }

        [RelayCommand]
        public void OpenExportWindow()
        {
            if (SelectedPhotos.Count > 0)
            {
                var selectedIds = SelectedPhotos.Select(p => p.PhotoId).ToList();
                var exportWindow = new Views.ExportWindow(selectedIds, App.ConnectionString);
                exportWindow.Owner = Application.Current.MainWindow;
                exportWindow.ShowDialog();
            }
            else if (SelectedFolder != null && Photos.Count > 0)
            {
                var result = MessageBox.Show(
                    $"Экспортировать всю папку \"{SelectedFolder.FolderName}\" ({Photos.Count} фото)?",
                    "Экспорт папки",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    var exportWindow = new Views.ExportWindow(SelectedFolder.FolderId, SelectedFolder.FolderName ?? "Папка", App.ConnectionString);
                    exportWindow.Owner = Application.Current.MainWindow;
                    exportWindow.ShowDialog();
                }
            }
            else
            {
                MessageBox.Show(
                    "Нет фото для экспорта.\n\n" +
                    "Сначала выберите фотографии или откройте папку с фото.",
                    "Экспорт",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }

            _ = LoadDataAsync();
        }

        [RelayCommand]
        public async Task ExportSinglePhotoAsync(Photo? photo)
        {
            if (photo == null)
            {
                MessageBox.Show("Фото не выбрано!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Создаем список с одним фото
            var selectedIds = new List<int> { photo.PhotoId };

            var exportWindow = new Views.ExportWindow(selectedIds, App.ConnectionString);
            exportWindow.Owner = Application.Current.MainWindow;
            exportWindow.ShowDialog();

            await Task.CompletedTask;
        }
        [RelayCommand]
        public void ShareFolder(Folder? folder)
        {
            if (folder != null)
                Views.ShareDialog.ShowShareDialog(folder);
        }

        [RelayCommand]
        public void SharePhoto(Photo? photo)
        {
            if (photo != null)
                Views.ShareDialog.ShowShareDialog(photo);
        }

        [RelayCommand]
        public void SelectAllPhotos()
        {
            SelectedPhotos = new ObservableCollection<Photo>(Photos);
        }

        [RelayCommand]
        public void ClearSelection()
        {
            SelectedPhotos.Clear();
        }

        [RelayCommand]
        public void SelectPhoto(Photo? photo)
        {
            if (photo == null) return;

            if (SelectedPhotos.Contains(photo))
                SelectedPhotos.Remove(photo);
            else
                SelectedPhotos.Add(photo);
        }

        [RelayCommand]
        public void RenameFolder(Folder? folder)
        {
            if (folder == null) return;

            string? newName = Microsoft.VisualBasic.Interaction.InputBox(
                "Новое имя:", "Переименование", folder.FolderName ?? "");

            if (!string.IsNullOrWhiteSpace(newName))
            {
                folder.FolderName = newName;
                _ = LoadDataAsync();
            }
        }

        [RelayCommand]
        public async Task RefreshAsync()
        {
            if (SelectedFolder != null)
            {
                var photos = await _fileService.GetPhotosInFolderAsync(SelectedFolder.FolderId);
                Photos = new ObservableCollection<Photo>(photos);
                UpdateCounters();
                UpdateStorageInfo();
            }
            else
            {
                await LoadDataAsync();
            }
        }

        [RelayCommand]
        public async Task SearchPhotosAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                await RefreshAsync();
                return;
            }

            string search = SearchText.Trim();
            var source = Photos.Any()
                ? Photos
                : new ObservableCollection<Photo>(SelectedFolder != null
                    ? await _fileService.GetPhotosInFolderAsync(SelectedFolder.FolderId)
                    : Enumerable.Empty<Photo>());

            var filtered = source.Where(p =>
                p.OriginalName?.Contains(search, StringComparison.OrdinalIgnoreCase) == true ||
                p.FileName?.Contains(search, StringComparison.OrdinalIgnoreCase) == true ||
                p.Tags?.Contains(search, StringComparison.OrdinalIgnoreCase) == true).ToList();

            Photos = new ObservableCollection<Photo>(filtered);
            UpdateCounters();
            StatusText = $"Найдено: {filtered.Count}";
        }

        [RelayCommand]
        public void Logout()
        {
            var result = MessageBox.Show("Выйти?", "Выход",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                App.CurrentUserId = 0;
                App.CurrentUsername = null;

                new LoginWindow().Show();
                Application.Current.Windows.OfType<Window>()
                    .FirstOrDefault(w => w is Views.MainWindow)?.Close();
            }
        }

        private void UpdateStorageInfo()
        {
            StorageUsed = Photos.Sum(p => p.FileSize);
            StoragePercent = (double)StorageUsed / StorageLimit * 100;
            StorageDisplay = $"{FormatSize(StorageUsed)} / {FormatSize(StorageLimit)}";
        }

        private void UpdateCounters()
        {
            TotalPhotos = Photos.Count;
            TotalFolders = Folders.Count;
        }

        private static string FormatSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double value = bytes;
            int index = 0;
            while (value >= 1024 && index < sizes.Length - 1)
            {
                value /= 1024;
                index++;
            }

            return $"{value:0.#} {sizes[index]}";
        }

        partial void OnSearchTextChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                _ = RefreshAsync();
        }
    }
}
