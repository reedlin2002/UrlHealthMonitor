using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;

namespace UrlHealthMonitorApp
{
    public class Database
    {
        private readonly string _connectionString;

        public Database(string dbPath)
        {
            _connectionString = $"Data Source={dbPath}";
            EnsureTableExists();
        }

        private void EnsureTableExists()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            connection.Execute(@"
                CREATE TABLE IF NOT EXISTS CheckResults (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Url TEXT NOT NULL,
                    StatusCode INTEGER,
                    ResponseTimeMs INTEGER,
                    CheckedAt TEXT NOT NULL
                )
            ");
        }

        public async Task InsertResultAsync(string url, int statusCode, long responseTimeMs, DateTime checkedAt)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.ExecuteAsync(@"
                INSERT INTO CheckResults (Url, StatusCode, ResponseTimeMs, CheckedAt)
                VALUES (@Url, @StatusCode, @ResponseTimeMs, @CheckedAt)
            ", new
            {
                Url = url,
                StatusCode = statusCode,
                ResponseTimeMs = responseTimeMs,
                CheckedAt = checkedAt.ToString("o")
            });
        }
    }
}
