using Microsoft.Data.Sqlite;
using System.Globalization;
using System.IO;

namespace AtelierManager.Services
{
    public static class DatabaseBackupService
    {
        private const string DatabaseFileName = "atelier.db";
        private const string PendingRestoreFileName = "atelier_restore_pending.db";
        private const string BackupFolderName = "Резервные копии";
        private const string LegacyBackupFolderName = "backups";

        public static string DataDirectory =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");

        public static string DatabasePath =>
            Path.Combine(DataDirectory, DatabaseFileName);

        public static string BackupDirectory =>
            Path.Combine(DataDirectory, BackupFolderName);

        public static string LegacyBackupDirectory =>
            Path.Combine(DataDirectory, LegacyBackupFolderName);

        public static string PendingRestorePath =>
            Path.Combine(DataDirectory, PendingRestoreFileName);

        public static string CreateBackup()
        {
            Directory.CreateDirectory(DataDirectory);
            Directory.CreateDirectory(BackupDirectory);

            if (!File.Exists(DatabasePath))
                throw new FileNotFoundException("Файл базы данных не найден.", DatabasePath);

            var backupPath = CreateRussianBackupPath("Резервная копия");

            using var connection = new SqliteConnection($"Data Source={DatabasePath};Foreign Keys=True;");
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "VACUUM INTO @backupPath";
            command.Parameters.AddWithValue("@backupPath", backupPath);
            command.ExecuteNonQuery();

            return backupPath;
        }

        public static void PrepareRestore(string backupPath)
        {
            if (!File.Exists(backupPath))
                throw new FileNotFoundException("Файл резервной копии не найден.", backupPath);

            Directory.CreateDirectory(DataDirectory);
            File.Copy(backupPath, PendingRestorePath, overwrite: true);
        }

        public static bool ApplyPendingRestoreIfExists()
        {
            Directory.CreateDirectory(DataDirectory);
            Directory.CreateDirectory(BackupDirectory);

            if (!File.Exists(PendingRestorePath))
                return false;

            if (File.Exists(DatabasePath))
            {
                var beforeRestorePath = CreateRussianBackupPath("Копия перед переключением");

                File.Copy(DatabasePath, beforeRestorePath, overwrite: false);
                File.Delete(DatabasePath);
            }

            File.Move(PendingRestorePath, DatabasePath);
            return true;
        }

        public static IReadOnlyList<FileInfo> GetBackups()
        {
            Directory.CreateDirectory(BackupDirectory);

            return GetKnownBackupDirectories()
                .Where(Directory.Exists)
                .SelectMany(directory => new DirectoryInfo(directory).GetFiles("*.db"))
                .OrderByDescending(x => x.LastWriteTime)
                .ToList();
        }

        public static void DeleteBackup(string backupPath)
        {
            if (string.IsNullOrWhiteSpace(backupPath))
                throw new ArgumentException("Не указан файл резервной копии.", nameof(backupPath));

            var fullPath = Path.GetFullPath(backupPath);

            if (!GetKnownBackupDirectories().Any(directory => IsFileInsideDirectory(fullPath, directory)))
                throw new InvalidOperationException("Можно удалять только файлы из папки резервных копий.");

            if (!File.Exists(fullPath))
                throw new FileNotFoundException("Файл резервной копии не найден.", fullPath);

            if (!string.Equals(Path.GetExtension(fullPath), ".db", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Можно удалять только файлы базы данных с расширением .db.");

            File.Delete(fullPath);
        }

        private static string CreateRussianBackupPath(string readablePrefix)
        {
            var timestamp = DateTime.Now.ToString("dd.MM.yyyy HH-mm-ss", CultureInfo.GetCultureInfo("ru-RU"));
            var baseFileName = $"{readablePrefix} от {timestamp}";
            var backupPath = Path.Combine(BackupDirectory, $"{baseFileName}.db");

            var copyNumber = 2;
            while (File.Exists(backupPath))
            {
                backupPath = Path.Combine(BackupDirectory, $"{baseFileName} ({copyNumber}).db");
                copyNumber++;
            }

            return backupPath;
        }

        private static IEnumerable<string> GetKnownBackupDirectories()
        {
            yield return BackupDirectory;
            yield return LegacyBackupDirectory;
        }

        private static bool IsFileInsideDirectory(string filePath, string directoryPath)
        {
            var normalizedDirectory = Path.GetFullPath(directoryPath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;

            return filePath.StartsWith(normalizedDirectory, StringComparison.OrdinalIgnoreCase);
        }
    }
}
