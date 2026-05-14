using System.Windows;
using Diploma.ViewModels;

namespace Diploma.Views
{
    public partial class ImportWindow : Window
    {
        public ImportWindow()
        {
            InitializeComponent();
        }

        public ImportWindow(int targetFolderId, string targetFolderName)
        {
            InitializeComponent();
            DataContext = new ImportViewModel(targetFolderId, targetFolderName);
        }
    }
}
