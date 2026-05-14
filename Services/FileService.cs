using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Dapper;
using Diploma.Models;

namespace Diploma.Services
{
    public class FileService
    {
        private readonly string _connectionString;

        public FileService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<IEnumerable<Folder>> GetFoldersAsync(int userId, int? parentFolderId = null)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
                SELECT * FROM Folders 
                WHERE UserId = @UserId 
                AND (@ParentFolderId IS NULL AND ParentFolderId IS NULL OR ParentFolderId = @ParentFolderId)
                ORDER BY FolderName";

            return await connection.QueryAsync<Folder>(sql,
                new { UserId = userId, ParentFolderId = parentFolderId });
        }

        public async Task<IEnumerable<Photo>> GetPhotosInFolderAsync(int folderId)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
                SELECT PhotoId, FolderId, UserId, FileName, OriginalName, 
                       ThumbnailData, Tags, FileSize, Width, Height, UploadedAt, IsPublic
                FROM Photos 
                WHERE FolderId = @FolderId
                ORDER BY UploadedAt DESC";

            return await connection.QueryAsync<Photo>(sql, new { FolderId = folderId });
        }

        public async Task<int> CreateFolderAsync(int userId, string folderName, int? parentFolderId = null)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
                INSERT INTO Folders (UserId, ParentFolderId, FolderName)
                VALUES (@UserId, @ParentFolderId, @FolderName);
                SELECT LAST_INSERT_ID();";

            return await connection.ExecuteScalarAsync<int>(sql, new
            {
                UserId = userId,
                ParentFolderId = parentFolderId,
                FolderName = folderName
            });
        }

        public async Task DeletePhotoAsync(int photoId)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            await connection.ExecuteAsync(
                "DELETE FROM Photos WHERE PhotoId = @PhotoId",
                new { PhotoId = photoId });
        }

        public async Task DeleteFolderAsync(int folderId)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            await connection.ExecuteAsync(
                "DELETE FROM Folders WHERE FolderId = @FolderId",
                new { FolderId = folderId });
        }

        public async Task<string> CreateShareLinkAsync(int? folderId, int? photoId, int userId, DateTime? expiryDate = null)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string shareCode = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                .Replace("/", "_")
                .Replace("+", "-")[..12];

            string sql = @"
                INSERT INTO ShareLinks (FolderId, PhotoId, ShareCode, CreatedBy, ExpiryDate)
                VALUES (@FolderId, @PhotoId, @ShareCode, @CreatedBy, @ExpiryDate)";

            await connection.ExecuteAsync(sql, new
            {
                FolderId = folderId,
                PhotoId = photoId,
                ShareCode = shareCode,
                CreatedBy = userId,
                ExpiryDate = expiryDate
            });

            return shareCode;
        }

        public async Task<IEnumerable<Photo>> GetSharedPhotosAsync(string shareCode)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
                SELECT p.* 
                FROM Photos p
                JOIN ShareLinks sl ON (sl.PhotoId = p.PhotoId OR sl.FolderId = p.FolderId)
                WHERE sl.ShareCode = @ShareCode 
                AND sl.IsActive = TRUE 
                AND (sl.ExpiryDate IS NULL OR sl.ExpiryDate > NOW())";

            return await connection.QueryAsync<Photo>(sql, new { ShareCode = shareCode });
        }

        public async Task<Photo?> GetPhotoByIdAsync(int photoId)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            return await connection.QueryFirstOrDefaultAsync<Photo>(
                "SELECT * FROM Photos WHERE PhotoId = @PhotoId",
                new { PhotoId = photoId });
        }

        public async Task<Folder?> GetFolderByIdAsync(int folderId)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            return await connection.QueryFirstOrDefaultAsync<Folder>(
                "SELECT * FROM Folders WHERE FolderId = @FolderId",
                new { FolderId = folderId });
        }
    }
}
