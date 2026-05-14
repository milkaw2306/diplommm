using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Diploma.ViewModels;
using Diploma.Models;

namespace Diploma.Views
{
    public partial class MainWindow : Window
    {
        private MainViewModel? ViewModel => DataContext as MainViewModel;

        public MainWindow()
        {
            InitializeComponent();
            KeyDown += Window_KeyDown;
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы уверены, что хотите выйти?", "Выход",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                new LoginWindow().Show();
                this.Close();
            }
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Photo Drive v1.0\n\n" +
                "Приложение для портфолио фотографов\n" +
                "Дипломный проект\n\n" +
                "© 2026 Все права защищены",
                "О программе",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void PhotoContextMenu_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Photo photo)
            {
                var contextMenu = new ContextMenu();

                var viewItem = new MenuItem { Header = "Просмотр" };
                viewItem.Click += (s, args) => ViewPhoto(photo);

                var shareItem = new MenuItem { Header = "Поделиться" };
                shareItem.Click += (s, args) => SharePhoto(photo);

                var deleteItem = new MenuItem { Header = "Удалить" };
                deleteItem.Click += (s, args) => ViewModel?.DeletePhotoCommand.Execute(photo);

                contextMenu.Items.Add(viewItem);
                contextMenu.Items.Add(new Separator());
                contextMenu.Items.Add(shareItem);
                contextMenu.Items.Add(new Separator());
                contextMenu.Items.Add(deleteItem);

                contextMenu.IsOpen = true;
            }
        }

        private void ViewPhoto(Photo photo)
        {
            MessageBox.Show($"Просмотр: {photo.OriginalName}\n{photo.Dimensions}\n{photo.SizeDisplay}",
                "Просмотр", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SharePhoto(Photo photo)
        {
            ShareDialog.ShowShareDialog(photo);
        }

        private void FolderContextMenu_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Folder folder)
            {
                var contextMenu = new ContextMenu();

                var openItem = new MenuItem { Header = "Открыть" };
                openItem.Click += (s, args) => ViewModel?.NavigateToFolderCommand.Execute(folder);

                var shareItem = new MenuItem { Header = "Поделиться" };
                shareItem.Click += (s, args) => ShareDialog.ShowShareDialog(folder);

                var deleteItem = new MenuItem { Header = "Удалить" };
                deleteItem.Click += (s, args) => ViewModel?.DeleteFolderCommand.Execute(folder);

                contextMenu.Items.Add(openItem);
                contextMenu.Items.Add(new Separator());
                contextMenu.Items.Add(shareItem);
                contextMenu.Items.Add(new Separator());
                contextMenu.Items.Add(deleteItem);

                contextMenu.IsOpen = true;
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                switch (e.Key)
                {
                    case Key.N:
                        ViewModel?.CreateFolderCommand.Execute(null);
                        break;
                    case Key.I:
                        ViewModel?.OpenImportWindow();
                        break;
                    case Key.E:
                        ViewModel?.OpenExportWindow();
                        break;
                    case Key.A:
                        ViewModel?.SelectAllPhotos();
                        break;
                    case Key.F5:
                        ViewModel?.RefreshCommand.Execute(null);
                        break;
                }
            }
        }
    }
}
