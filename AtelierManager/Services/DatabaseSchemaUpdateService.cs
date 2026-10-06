using System;
using System.IO;
using AtelierManager.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AtelierManager.Services
{
    public static class DatabaseSchemaUpdateService
    {
        public static void EnsureUpdated()
        {
            using (var db = new AtelierContext())
            {
                db.Database.EnsureCreated();
            }

            var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "atelier.db");
            Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

            using var connection = new SqliteConnection($"Data Source={dbPath};Foreign Keys=True;");
            connection.Open();

            Execute(connection, "PRAGMA foreign_keys = ON;");

            Execute(connection, @"
            CREATE TABLE IF NOT EXISTS Roles (
                Id INTEGER NOT NULL CONSTRAINT PK_Roles PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                DisplayName TEXT NOT NULL
            );");

            Execute(connection, @"
            CREATE UNIQUE INDEX IF NOT EXISTS IX_Roles_Name ON Roles(Name);");

            Execute(connection, @"
            CREATE TABLE IF NOT EXISTS Employees (
                Id INTEGER NOT NULL CONSTRAINT PK_Employees PRIMARY KEY AUTOINCREMENT,
                FullName TEXT NOT NULL,
                Login TEXT NOT NULL,
                Password TEXT NOT NULL,
                RoleId INTEGER NOT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1,
                CONSTRAINT FK_Employees_Roles_RoleId FOREIGN KEY (RoleId) REFERENCES Roles(Id) ON DELETE RESTRICT
            );");

            Execute(connection, @"
            CREATE UNIQUE INDEX IF NOT EXISTS IX_Employees_Login ON Employees(Login);");

            Execute(connection, @"
            CREATE INDEX IF NOT EXISTS IX_Employees_RoleId ON Employees(RoleId);");

            Execute(connection, "INSERT OR IGNORE INTO Roles (Id, Name, DisplayName) VALUES (1, 'Admin', 'Администратор');");
            Execute(connection, "INSERT OR IGNORE INTO Roles (Id, Name, DisplayName) VALUES (2, 'Worker', 'Работник');");

            Execute(connection, @"
            INSERT OR IGNORE INTO Employees (Id, FullName, Login, Password, RoleId, IsActive)
            VALUES (1, 'Администратор', 'admin', 'admin', 1, 1);");

            SeedDefaultStatuses(connection);
            EnsureOrdersEmployeeColumn(connection);
        }


        private static void SeedDefaultStatuses(SqliteConnection connection)
        {
            if (!TableExists(connection, "Statuses"))
                return;

            Execute(connection, "INSERT OR IGNORE INTO Statuses (Id, Name, SortOrder, IsFinal) VALUES (1, 'Принят', 1, 0);");
            Execute(connection, "INSERT OR IGNORE INTO Statuses (Id, Name, SortOrder, IsFinal) VALUES (2, 'В работе', 2, 0);");
            Execute(connection, "INSERT OR IGNORE INTO Statuses (Id, Name, SortOrder, IsFinal) VALUES (3, 'Готов', 3, 0);");
            Execute(connection, "INSERT OR IGNORE INTO Statuses (Id, Name, SortOrder, IsFinal) VALUES (4, 'Выдан', 4, 1);");
            Execute(connection, "INSERT OR IGNORE INTO Statuses (Id, Name, SortOrder, IsFinal) VALUES (5, 'Отменён', 5, 1);");
        }

        private static void EnsureOrdersEmployeeColumn(SqliteConnection connection)
        {
            if (!TableExists(connection, "Orders"))
                return;

            if (!ColumnExists(connection, "Orders", "EmployeeId"))
            {
                Execute(connection, "ALTER TABLE Orders ADD COLUMN EmployeeId INTEGER NULL;");
            }

            Execute(connection, "UPDATE Orders SET EmployeeId = 1 WHERE EmployeeId IS NULL;");

            Execute(connection, "CREATE INDEX IF NOT EXISTS IX_Orders_EmployeeId ON Orders(EmployeeId);");
        }

        private static bool TableExists(SqliteConnection connection, string tableName)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $name;";
            command.Parameters.AddWithValue("$name", tableName);
            return Convert.ToInt32(command.ExecuteScalar()) > 0;
        }

        private static bool ColumnExists(SqliteConnection connection, string tableName, string columnName)
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"PRAGMA table_info({tableName});";
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                if (string.Equals(reader["name"]?.ToString(), columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static void Execute(SqliteConnection connection, string sql)
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }
    }
}
