using Microsoft.Data.Sqlite;
using System.IO;

namespace AtelierManager.Services
{
    public static class StockMaintenanceService
    {
        private const string MarkerKey = "StockNormalizedForOrderMaterials";

        public static void NormalizeInitialMaterialStockIfNeeded()
        {
            Directory.CreateDirectory(DatabaseBackupService.DataDirectory);

            if (!File.Exists(DatabaseBackupService.DatabasePath)) return;

            using var connection = new SqliteConnection($"Data Source={DatabaseBackupService.DatabasePath};Foreign Keys=True;");
            connection.Open();

            using var transaction = connection.BeginTransaction();

            ExecuteNonQuery(connection, transaction, "CREATE TABLE IF NOT EXISTS AppMetadata (Key TEXT PRIMARY KEY, Value TEXT NOT NULL);");

            using (var markerCommand = connection.CreateCommand())
            {
                markerCommand.Transaction = transaction;
                markerCommand.CommandText = "SELECT Value FROM AppMetadata WHERE Key = @key LIMIT 1;";
                markerCommand.Parameters.AddWithValue("@key", MarkerKey);

                var marker = markerCommand.ExecuteScalar();
                if (marker != null)
                {
                    transaction.Commit();
                    return;
                }
            }

            ExecuteNonQuery(connection, transaction,
                @"UPDATE Materials
                SET Quantity = Quantity - COALESCE((
                    SELECT SUM(QtyUsed)
                    FROM OrderMaterials
                    WHERE OrderMaterials.MaterialId = Materials.Id
                ), 0);");

            ExecuteNonQuery(connection, transaction,
                "INSERT OR REPLACE INTO AppMetadata(Key, Value) VALUES(@key, '1');",
                ("@key", MarkerKey));

            transaction.Commit();
        }

        private static void ExecuteNonQuery(
            SqliteConnection connection,
            SqliteTransaction transaction,
            string sql,
            params (string Name, object Value)[] parameters)
            {
                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = sql;

                foreach (var parameter in parameters)
                    command.Parameters.AddWithValue(parameter.Name, parameter.Value);

                command.ExecuteNonQuery();
            }
    }
}
