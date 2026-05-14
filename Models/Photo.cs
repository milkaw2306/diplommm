namespace Diploma.Models
{
    public class Photo : INotifyPropertyChanged
    {
        public int PhotoId { get; set; }
        public int FolderId { get; set; }
        public int UserId { get; set; }
        public string? FileName { get; set; }
        public string? OriginalName { get; set; }
        public byte[]? FileData { get; set; }
        public byte[]? ThumbnailData { get; set; }
        public string? Tags { get; set; }
        public long FileSize { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.Now;
        public bool IsPublic { get; set; }

        private BitmapImage? _thumbnail;
        public BitmapImage? Thumbnail
        {
            get
            {
                if (_thumbnail == null)
                {
                    _thumbnail = Helpers.ImageHelper.BytesToImage(ThumbnailData)
                        ?? Helpers.ImageHelper.BytesToImage(FileData);
                }

                return _thumbnail;
            }
            set { _thumbnail = value; OnPropertyChanged(); }
        }

        public string SizeDisplay => FormatSize(FileSize);
        public string Dimensions => $"{Width}x{Height}";
        public IEnumerable<string> TagList => string.IsNullOrWhiteSpace(Tags)
            ? Enumerable.Empty<string>()
            : Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        private string FormatSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
