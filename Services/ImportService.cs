using System.Windows.Media.Imaging;
using Dapper;
using MySql.Data.MySqlClient;

namespace Diploma.Services
{
    public class ImportService
    {
        private readonly string _connectionString;
        private readonly int _currentUserId;
        private static readonly string[] SupportedFormats = { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };

        public event EventHandler<ImportProgressEventArgs>? ProgressChanged;
        public event EventHandler? ImportCompleted;

        public ImportService(string connectionString, int userId)
        {
            _connectionString = connectionString;
            _currentUserId = userId;
        }

        public async Task<int> ImportDraggedFiles(string[] filePaths, int targetFolderId, string tags)
        {
            string normalizedTags = ValidateImportTarget(targetFolderId, tags);
            var imageFiles = filePaths
                .Where(f => SupportedFormats.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .ToArray();

            if (imageFiles.Length == 0)
                throw new InvalidOperationException("В выбранных файлах нет поддерживаемых изображений.");

            int count = 0;
            for (int i = 0; i < imageFiles.Length; i++)
            {
                await ImportSinglePhoto(imageFiles[i], targetFolderId, normalizedTags);
                count++;

                ProgressChanged?.Invoke(this, new ImportProgressEventArgs
                {
                    CurrentFile = Path.GetFileName(imageFiles[i]),
                    ProcessedCount = count,
                    TotalCount = imageFiles.Length
                });
            }

            ImportCompleted?.Invoke(this, EventArgs.Empty);
            return count;
        }

        public async Task<int> ImportFolderWithStructure(string folderPath, int targetFolderId, string tags)
        {
            string normalizedTags = ValidateImportTarget(targetFolderId, tags);
            var files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
                .Where(f => SupportedFormats.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .ToList();

            if (files.Count == 0)
                throw new InvalidOperationException("В выбранной папке нет поддерживаемых изображений.");

            int count = 0;
            for (int i = 0; i < files.Count; i++)
            {
                await ImportSinglePhoto(files[i], targetFolderId, normalizedTags);
                count++;

                ProgressChanged?.Invoke(this, new ImportProgressEventArgs
                {
                    CurrentFile = Path.GetFileName(files[i]),
                    ProcessedCount = count,
                    TotalCount = files.Count
                });
            }

            ImportCompleted?.Invoke(this, EventArgs.Empty);
            return count;
        }

        private async Task ImportSinglePhoto(string filePath, int folderId, string tags)
        {
            try
            {
                byte[] data = await File.ReadAllBytesAsync(filePath);
                var info = new FileInfo(filePath);
                var imageMetadata = CreateImageMetadata(filePath);

                using var conn = new MySqlConnection(_connectionString);
                await conn.OpenAsync();

                await conn.ExecuteAsync(@"
                    INSERT INTO Photos (FolderId, UserId, FileName, OriginalName, FileData, ThumbnailData, Tags, FileSize, Width, Height)
                    VALUES (@FolderId, @UserId, @FileName, @OriginalName, @FileData, @ThumbnailData, @Tags, @FileSize, @Width, @Height)",
                    new
                    {
                        FolderId = folderId,
                        UserId = _currentUserId,
                        FileName = Guid.NewGuid() + Path.GetExtension(filePath),
                        OriginalName = Path.GetFileName(filePath),
                        FileData = data,
                        ThumbnailData = imageMetadata.ThumbnailData,
                        Tags = tags,
                        FileSize = info.Length,
                        Width = imageMetadata.Width,
                        Height = imageMetadata.Height
                    });
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Не удалось импортировать \"{Path.GetFileName(filePath)}\": {ex.Message}", ex);
            }
        }

        private static string ValidateImportTarget(int targetFolderId, string tags)
        {
            if (targetFolderId <= 0)
                throw new InvalidOperationException("Перед импортом выберите папку, куда будут добавлены фотографии.");

            string normalizedTags = NormalizeTags(tags);
            if (string.IsNullOrWhiteSpace(normalizedTags))
                throw new InvalidOperationException("Добавьте теги для импортируемых фотографий.");

            return normalizedTags;
        }

        private static string NormalizeTags(string tags)
        {
            return string.Join(", ",
                tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(t => t.Trim().TrimStart('#'))
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Distinct(StringComparer.OrdinalIgnoreCase));
        }

        private static ImageMetadata CreateImageMetadata(string filePath)
        {
            using var originalStream = File.OpenRead(filePath);
            var decoder = BitmapDecoder.Create(originalStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            int width = decoder.Frames[0].PixelWidth;
            int height = decoder.Frames[0].PixelHeight;

            using var thumbnailStream = File.OpenRead(filePath);
            var thumbnail = new BitmapImage();
            thumbnail.BeginInit();
            thumbnail.CacheOption = BitmapCacheOption.OnLoad;
            thumbnail.DecodePixelWidth = 520;
            thumbnail.StreamSource = thumbnailStream;
            thumbnail.EndInit();
            thumbnail.Freeze();

            using var output = new MemoryStream();
            var encoder = new JpegBitmapEncoder { QualityLevel = 84 };
            encoder.Frames.Add(BitmapFrame.Create(thumbnail));
            encoder.Save(output);

            return new ImageMetadata(width, height, output.ToArray());
        }

        private sealed record ImageMetadata(int Width, int Height, byte[] ThumbnailData);
    }

    public class ImportProgressEventArgs : EventArgs
    {
        public string? CurrentFile { get; set; }
        public int ProcessedCount { get; set; }
        public int TotalCount { get; set; }
    }
}
