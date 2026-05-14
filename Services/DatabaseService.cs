using System.Data;
using MySql.Data.MySqlClient;
using Dapper;

namespace Diploma.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<IDbConnection> GetConnectionAsync()
        {
            var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            return connection;
        }

        public async Task InitializeDatabaseAsync()
        {
            using var connection = new MySqlConnection(_connectionString.Replace("database=diploma;", ""));
            await connection.OpenAsync();

            await connection.ExecuteAsync("CREATE DATABASE IF NOT EXISTS diploma CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;");
            await connection.ExecuteAsync("USE diploma;");
            await CreateTablesAsync(connection);
        }

        private async Task CreateTablesAsync(MySqlConnection connection)
        {
            await connection.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS Users (
                    UserId INT AUTO_INCREMENT PRIMARY KEY,
                    Username VARCHAR(50) UNIQUE NOT NULL,
                    Email VARCHAR(100) UNIQUE NOT NULL,
                    PasswordHash VARCHAR(255) NOT NULL,
                    FullName VARCHAR(100),
                    Avatar LONGBLOB,
                    StorageLimit BIGINT DEFAULT 10737418240,
                    StorageUsed BIGINT DEFAULT 0,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    LastLogin DATETIME,
                    ResetCode VARCHAR(6),
                    ResetCodeExpiry DATETIME,
                    IsActive BOOLEAN DEFAULT TRUE
                ) ENGINE=InnoDB;
            ");

            await connection.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS Folders (
                    FolderId INT AUTO_INCREMENT PRIMARY KEY,
                    UserId INT NOT NULL,
                    ParentFolderId INT NULL,
                    FolderName VARCHAR(255) NOT NULL,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    IsPublic BOOLEAN DEFAULT FALSE,
                    FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE,
                    FOREIGN KEY (ParentFolderId) REFERENCES Folders(FolderId) ON DELETE CASCADE
                ) ENGINE=InnoDB;
            ");

            await connection.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS Photos (
                    PhotoId INT AUTO_INCREMENT PRIMARY KEY,
                    FolderId INT NOT NULL,
                    UserId INT NOT NULL,
                    FileName VARCHAR(255) NOT NULL,
                    OriginalName VARCHAR(255) NOT NULL,
                    FileData LONGBLOB NOT NULL,
                    ThumbnailData MEDIUMBLOB,
                    Tags TEXT,
                    FileSize BIGINT NOT NULL,
                    Width INT,
                    Height INT,
                    UploadedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    IsPublic BOOLEAN DEFAULT FALSE,
                    FOREIGN KEY (FolderId) REFERENCES Folders(FolderId) ON DELETE CASCADE,
                    FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE
                ) ENGINE=InnoDB;
            ");

            await AddColumnIfMissingAsync(connection, "Photos", "Tags", "TEXT");

            await connection.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS ShareLinks (
                    ShareId INT AUTO_INCREMENT PRIMARY KEY,
                    FolderId INT NULL,
                    PhotoId INT NULL,
                    ShareCode VARCHAR(20) UNIQUE NOT NULL,
                    CreatedBy INT NOT NULL,
                    ExpiryDate DATETIME NULL,
                    IsActive BOOLEAN DEFAULT TRUE,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (FolderId) REFERENCES Folders(FolderId) ON DELETE CASCADE,
                    FOREIGN KEY (PhotoId) REFERENCES Photos(PhotoId) ON DELETE CASCADE,
                    FOREIGN KEY (CreatedBy) REFERENCES Users(UserId) ON DELETE CASCADE
                ) ENGINE=InnoDB;
            ");

            await connection.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS ExportHistory (
                    ExportId INT AUTO_INCREMENT PRIMARY KEY,
                    UserId INT NOT NULL,
                    ExportPath VARCHAR(500) NOT NULL,
                    PhotosCount INT NOT NULL,
                    TotalSize BIGINT NOT NULL,
                    ExportType VARCHAR(50) DEFAULT 'original',
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE
                ) ENGINE=InnoDB;
            ");

            await connection.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS ImportHistory (
                    ImportId INT AUTO_INCREMENT PRIMARY KEY,
                    UserId INT NOT NULL,
                    ImportPath VARCHAR(500) NOT NULL,
                    PhotosCount INT NOT NULL,
                    TotalSize BIGINT NOT NULL,
                    Status VARCHAR(50) DEFAULT 'completed',
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    CompletedAt DATETIME,
                    FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE
                ) ENGINE=InnoDB;
            ");
        }

        private static async Task AddColumnIfMissingAsync(MySqlConnection connection, string tableName, string columnName, string columnDefinition)
        {
            int columnExists = await connection.ExecuteScalarAsync<int>(@"
                SELECT COUNT(*)
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_SCHEMA = DATABASE()
                  AND TABLE_NAME = @TableName
                  AND COLUMN_NAME = @ColumnName;",
                new { TableName = tableName, ColumnName = columnName });

            if (columnExists == 0)
            {
                await connection.ExecuteAsync($"ALTER TABLE `{tableName}` ADD COLUMN `{columnName}` {columnDefinition};");
            }
        }
    }
}
