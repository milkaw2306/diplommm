using System.Windows;
using Diploma.Models;

namespace Diploma.Views
{
    public partial class ShareDialog : Window
    {
        public ShareDialog()
        {
            InitializeComponent();
        }

        public ShareDialog(Folder folder) : this()
        {
            if (DataContext is ViewModels.ShareViewModel vm)
            {
                vm.InitializeWithFolder(folder);
            }
        }

        public ShareDialog(Photo photo) : this()
        {
            if (DataContext is ViewModels.ShareViewModel vm)
            {
                vm.InitializeWithPhoto(photo);
            }
        }

        public static void ShowShareDialog(Folder folder)
        {
            var dialog = new ShareDialog(folder);
            dialog.Owner = Application.Current.MainWindow;
            dialog.ShowDialog();
        }

        public static void ShowShareDialog(Photo photo)
        {
            var dialog = new ShareDialog(photo);
            dialog.Owner = Application.Current.MainWindow;
            dialog.ShowDialog();
        }
    }
}