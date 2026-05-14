using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Diploma.Models;
using Diploma.ViewModels;
using Diploma.Views;
using MaterialDesignThemes.Wpf;

namespace Diploma.Views
{
    public partial class MainWindow : Window
    {
        private MainViewModel? ViewModel => DataContext as MainViewModel;

        public MainWindow()
        {
            InitializeComponent();
        }

        // Выход из приложения
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы уверены, что хотите выйти?", "Выход",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                Application.Current.Shutdown();
            }
        }

        // Контекстное меню для папки (три точки)
        private void FolderContextMenu_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var folder = button?.DataContext as Folder;

            if (folder == null) return;

            var contextMenu = new ContextMenu();

            // Переименовать папку
            var renameItem = new MenuItem
            {
                Header = "✏️ Переименовать",
                Icon = new PackIcon { Kind = PackIconKind.Rename, Width = 18, Height = 18 }
            };
            renameItem.Click += (s, args) =>
            {
                ViewModel?.RenameFolderCommand.Execute(folder);
            };

            // Удалить папку
            var deleteItem = new MenuItem
            {
                Header = "🗑 Удалить папку",
                Icon = new PackIcon { Kind = PackIconKind.Delete, Width = 18, Height = 18 }
            };
            deleteItem.Click += (s, args) =>
            {
                ViewModel?.DeleteFolderCommand.Execute(folder);
            };

            // Поделиться папкой
            var shareItem = new MenuItem
            {
                Header = "🔗 Поделиться",
                Icon = new PackIcon { Kind = PackIconKind.Share, Width = 18, Height = 18 }
            };
            shareItem.Click += (s, args) =>
            {
                ViewModel?.ShareFolderCommand.Execute(folder);
            };

            contextMenu.Items.Add(renameItem);
            contextMenu.Items.Add(deleteItem);
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(shareItem);

            contextMenu.IsOpen = true;
        }

        // Контекстное меню для фото (три точки)
        private void PhotoContextMenu_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var photo = button?.DataContext as Photo;

            if (photo == null) return;

            var contextMenu = new ContextMenu();

            // Экспортировать фото (одно)
            var exportItem = new MenuItem
            {
                Header = "📥 Экспортировать фото",
                Icon = new PackIcon { Kind = PackIconKind.Download, Width = 18, Height = 18 }
            };
            exportItem.Click += (s, args) =>
            {
                ViewModel?.ExportSinglePhotoCommand.Execute(photo);
            };

            // Поделиться фото
            var shareItem = new MenuItem
            {
                Header = "🔗 Поделиться",
                Icon = new PackIcon { Kind = PackIconKind.Share, Width = 18, Height = 18 }
            };
            shareItem.Click += (s, args) =>
            {
                ViewModel?.SharePhotoCommand.Execute(photo);
            };

            // Удалить фото
            var deleteItem = new MenuItem
            {
                Header = "🗑 Удалить фото",
                Icon = new PackIcon { Kind = PackIconKind.Delete, Width = 18, Height = 18 }
            };
            deleteItem.Click += (s, args) =>
            {
                ViewModel?.DeletePhotoCommand.Execute(photo);
            };

            contextMenu.Items.Add(exportItem);
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(shareItem);
            contextMenu.Items.Add(deleteItem);

            contextMenu.IsOpen = true;
        }

        // Обработчик двойного клика по фото (можно открыть просмотр)
        private void PhotoDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var button = sender as Button;
            var photo = button?.DataContext as Photo;

            if (photo == null) return;

            // Здесь можно открыть окно просмотра фото
            // Например: new PhotoViewerWindow(photo).ShowDialog();
        }
    }
}