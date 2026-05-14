using MySql.Data.MySqlClient;
using Dapper;

namespace Diploma.Services
{
    public class ExportService
    {
        private readonly string _connectionString;

        public ExportService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public event EventHandler<int>? ProgressChanged;

        public async Task<bool> ExportPhotos(List<int> photoIds, string exportPath)
        {
            try
            {
                if (!Directory.Exists(exportPath))
                    Directory.CreateDirectory(exportPath);

                string datedFolder = Path.Combine(exportPath, $"PhotoExport_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}");
                Directory.CreateDirectory(datedFolder);

                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                int total = photoIds.Count;
                int processed = 0;

                foreach (int photoId in photoIds)
                {
                    var photo = await connection.QueryFirstOrDefaultAsync<Models.Photo>(
                        "SELECT PhotoId, FileData, OriginalName FROM Photos WHERE PhotoId = @PhotoId",
                        new { PhotoId = photoId });

                    if (photo?.FileData != null && !string.IsNullOrEmpty(photo.OriginalName))
                    {
                        // Очищаем имя файла от недопустимых символов
                        string safeFileName = string.Join("_", photo.OriginalName.Split(Path.GetInvalidFileNameChars()));
                        string exportFileName = Path.Combine(datedFolder, safeFileName);

                        // Проверяем уникальность имени
                        int counter = 1;
                        string originalFileName = exportFileName;
                        while (File.Exists(exportFileName))
                        {
                            string nameWithoutExt = Path.GetFileNameWithoutExtension(originalFileName);
                            string ext = Path.GetExtension(originalFileName);
                            exportFileName = Path.Combine(datedFolder, $"{nameWithoutExt}_{counter}{ext}");
                            counter++;
                        }

                        await File.WriteAllBytesAsync(exportFileName, photo.FileData);
                    }

                    processed++;
                    ProgressChanged?.Invoke(this, (processed * 100) / total);
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Export error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ExportFolder(int folderId, string exportPath)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var photoIds = await connection.QueryAsync<int>(
                "SELECT PhotoId FROM Photos WHERE FolderId = @FolderId",
                new { FolderId = folderId });

            var idsList = photoIds.ToList();

            if (!idsList.Any())
            {
                return false;
            }

            return await ExportPhotos(idsList, exportPath);
        }
    }
}