using System;
using System.Collections.Generic;
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
            EnsureTablesExist();
        }

        private void EnsureTablesExist()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            // 結果表
            connection.Execute(@"
                CREATE TABLE IF NOT EXISTS CheckResults (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Url TEXT NOT NULL,
                    StatusCode INTEGER,
                    ResponseTimeMs INTEGER,
                    CheckedAt TEXT NOT NULL
                );
            ");

            // 要監控的 URL
            connection.Execute(@"
                CREATE TABLE IF NOT EXISTS MonitoredUrls (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Url TEXT NOT NULL
                );
            ");
        }

        public async Task InsertResultAsync(string url, int statusCode, long responseTimeMs, DateTime checkedAt)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.ExecuteAsync(@"
                INSERT INTO CheckResults (Url, StatusCode, ResponseTimeMs, CheckedAt)
                VALUES (@Url, @StatusCode, @ResponseTimeMs, @CheckedAt);
            ", new
            {
                Url = url,
                StatusCode = statusCode,
                ResponseTimeMs = responseTimeMs,
                CheckedAt = checkedAt.ToString("o")
            });
        }

        public async Task<List<(int Id, string Url)>> GetMonitoredUrlsAsync()
        {
            using var connection = new SqliteConnection(_connectionString);
            var rows = await connection.QueryAsync<(int, string)>("SELECT Id, Url FROM MonitoredUrls;");
            return rows.AsList();
        }

        public async Task AddMonitoredUrlAsync(string url)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.ExecuteAsync("INSERT INTO MonitoredUrls (Url) VALUES (@Url);", new { Url = url });
        }

        public async Task RemoveMonitoredUrlAsync(int id)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.ExecuteAsync("DELETE FROM MonitoredUrls WHERE Id = @Id;", new { Id = id });
        }

        public async Task<List<dynamic>> GetLatestResultsAsync(int limit)
        {
            using var connection = new SqliteConnection(_connectionString);
            var rows = await connection.QueryAsync(@"
                SELECT Url, StatusCode, ResponseTimeMs, CheckedAt
                FROM CheckResults
                ORDER BY CheckedAt DESC
                LIMIT @Limit
            ", new { Limit = limit });
            return rows.AsList();
        }

        public async Task<List<dynamic>> GetResultsByUrlAsync(string url, int limit)
        {
            using var connection = new SqliteConnection(_connectionString);
            var rows = await connection.QueryAsync(@"
                SELECT Url, StatusCode, ResponseTimeMs, CheckedAt
                FROM CheckResults
                WHERE Url = @Url
                ORDER BY CheckedAt DESC
                LIMIT @Limit
            ", new { Url = url, Limit = limit });
            return rows.AsList();
        }

    }
}
