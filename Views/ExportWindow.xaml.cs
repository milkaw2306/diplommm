using System;
using System.Collections.Generic;
using System.Windows;
using Diploma.ViewModels;

namespace Diploma.Views
{
    public partial class ExportWindow : Window
    {
        // Конструктор для экспорта выбранных фото
        public ExportWindow(List<int> selectedPhotoIds, string connectionString)
        {
            InitializeComponent();
            var viewModel = new ExportViewModel(selectedPhotoIds, connectionString);
            DataContext = viewModel;
        }

        // Конструктор для экспорта папки
        public ExportWindow(int folderId, string folderName, string connectionString)
        {
            InitializeComponent();
            var viewModel = new ExportViewModel(folderId, folderName, connectionString);
            DataContext = viewModel;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}