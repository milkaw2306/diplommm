namespace Diploma.Models
{
    public class Folder
    {
        public int FolderId { get; set; }
        public int UserId { get; set; }
        public int? ParentFolderId { get; set; }
        public string? FolderName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsPublic { get; set; }

        public ObservableCollection<Folder> SubFolders { get; set; } = new();
        public ObservableCollection<Photo> Photos { get; set; } = new();
    }
}