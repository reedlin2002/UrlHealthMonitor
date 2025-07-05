using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace UrlHealthMonitorApp
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var dbPath = Environment.GetEnvironmentVariable("DATABASE_PATH") ?? "results.db";

            if (args.Length > 0 && args[0].ToLowerInvariant() == "serve")
            {
                var builder = WebApplication.CreateBuilder();

                builder.Services.AddSingleton<Database>(_ => new Database(dbPath));
                builder.Services.AddSingleton<StatusChecker>();
                builder.Services.AddHostedService<MonitorService>();

                var app = builder.Build();
                
                app.Urls.Add("http://0.0.0.0:5000");

                app.MapGet("/", () => Results.Content(@"
<!DOCTYPE html>
<html lang='zh-TW'>
<head>
    <meta charset='utf-8' />
    <title>UrlHealthMonitor Dashboard</title>
    <style>
        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen,
                Ubuntu, Cantarell, 'Open Sans', 'Helvetica Neue', sans-serif;
            background: #f0f2f5;
            margin: 0;
            padding: 1.5rem;
            color: #333;
        }
        h1 {
            margin-bottom: 0.2rem;
            color: #222;
        }
        p {
            color: #666;
            margin-top: 0;
            margin-bottom: 1rem;
        }
        .table-container {
            max-width: 960px;
            margin: 0 auto;
            background: white;
            border-radius: 8px;
            box-shadow: 0 2px 8px rgb(0 0 0 / 0.1);
            overflow: hidden;
            border: 1px solid #ddd;
        }
        table {
            border-collapse: collapse;
            width: 100%;
            table-layout: fixed;
        }
        thead {
            background-color: #007acc;
            color: #fff;
            position: sticky;
            top: 0;
            z-index: 10;
        }
        th, td {
            padding: 0.75rem 1rem;
            border-bottom: 1px solid #eee;
            text-align: left;
            overflow-wrap: break-word;
        }
        tbody tr:hover {
            background-color: #f9f9f9;
        }
        tbody tr td.status-200 {
            color: #28a745; /* 綠色 */
            font-weight: 600;
        }
        tbody tr td.status-other {
            color: #dc3545; /* 紅色 */
            font-weight: 600;
        }
        @media (max-width: 600px) {
            body {
                padding: 0.8rem;
            }
            .table-container {
                max-width: 100%;
                border-radius: 0;
                box-shadow: none;
                border: none;
            }
            table, thead, tbody, th, td, tr {
                display: block;
                width: 100%;
            }
            thead tr {
                position: relative;
            }
            tbody tr {
                margin-bottom: 1rem;
                border: 1px solid #ddd;
                border-radius: 6px;
                padding: 0.5rem;
            }
            tbody tr td {
                text-align: right;
                padding-left: 50%;
                position: relative;
                border: none;
                border-bottom: 1px solid #eee;
            }
            tbody tr td::before {
                content: attr(data-label);
                position: absolute;
                left: 1rem;
                width: 45%;
                padding-left: 0;
                font-weight: 600;
                text-align: left;
                color: #555;
            }
            tbody tr td:last-child {
                border-bottom: none;
            }
        }
    </style>
</head>
<body>
    <h1>UrlHealthMonitor Dashboard</h1>
    <p>最近 100 筆監控結果（每 30 秒自動刷新）</p>
    <div class='table-container'>
        <table aria-label='URL Health Check Results'>
            <thead>
                <tr>
                    <th>URL</th>
                    <th>狀態碼</th>
                    <th>耗時 (ms)</th>
                    <th>檢查時間</th>
                </tr>
            </thead>
            <tbody id='results-body'>
                <!-- 資料由 JS 動態插入 -->
            </tbody>
        </table>
    </div>

    <script>
        function formatDateTime(isoStr) {
            const d = new Date(isoStr);
            return d.toLocaleString('zh-TW', { hour12: false });
        }

        async function fetchResults() {
            try {
                const res = await fetch('/results');
                if (!res.ok) throw new Error('Network response was not ok');
                const data = await res.json();

                const tbody = document.getElementById('results-body');
                tbody.innerHTML = '';

                for (const row of data) {
                    const tr = document.createElement('tr');
                    const isOk = row.StatusCode === 200;
                    const statusClass = isOk ? 'status-200' : 'status-other';

                    tr.innerHTML = `
                        <td data-label='URL'>${row.Url}</td>
                        <td data-label='狀態碼' class='${statusClass}'>${row.StatusCode}</td>
                        <td data-label='耗時 (ms)'>${row.ResponseTimeMs}</td>
                        <td data-label='檢查時間'>${formatDateTime(row.CheckedAt)}</td>
                    `;
                    tbody.appendChild(tr);
                }
            } catch (error) {
                console.error('Fetch error:', error);
            }
        }

        fetchResults();
        setInterval(fetchResults, 30000);
    </script>
</body>
</html>
", "text/html"));

                app.MapGet("/results", async (Database db) =>
                {
                    var results = await db.GetLatestResultsAsync(100);
                    return Results.Json(results);
                });

                await app.RunAsync();
                return;
            }

            // Build Host (for Worker Service and CLI)
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<StatusChecker>();
                    services.AddSingleton<Database>(_ => new Database(dbPath));
                    services.AddHostedService<MonitorService>();
                })
                .Build();

            var db = host.Services.GetRequiredService<Database>();

            if (args.Length > 0)
            {
                var command = args[0].ToLowerInvariant();

                if (command == "list")
                {
                    var urls = await db.GetMonitoredUrlsAsync();
                    Console.WriteLine("目前監控 URL：");
                    foreach (var (id, url) in urls)
                        Console.WriteLine($"{id}: {url}");
                    return;
                }
                else if (command == "add" && args.Length >= 2)
                {
                    await db.AddMonitoredUrlAsync(args[1]);
                    Console.WriteLine($"已新增 URL：{args[1]}");
                    return;
                }
                else if (command == "remove" && args.Length >= 2 && int.TryParse(args[1], out var id))
                {
                    await db.RemoveMonitoredUrlAsync(id);
                    Console.WriteLine($"已移除 ID：{id}");
                    return;
                }
                else
                {
                    Console.WriteLine("指令錯誤，支援：list / add <url> / remove <id> / serve");
                    return;
                }
            }

            await host.RunAsync();
        }
    }
}
