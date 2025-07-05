using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace UrlHealthMonitorApp
{
    public class MonitorService : BackgroundService
    {
        private readonly StatusChecker _checker;
        private readonly Database _database;

        public MonitorService(StatusChecker checker, Database database)
        {
            _checker = checker;
            _database = database;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var urlRecords = await _database.GetMonitoredUrlsAsync();
                if (urlRecords.Count == 0)
                {
                    Console.WriteLine("⚠️ 沒有任何要監控的 URL，30 秒後再檢查...");
                }
                else
                {
                    Console.WriteLine($"[{DateTime.UtcNow}] 開始檢查，共 {urlRecords.Count} 個 URL...");

                    foreach (var (id, url) in urlRecords)
                    {
                        var result = await _checker.GetStatusCodeAsync(url);

                        if (result.statusCode.HasValue)
                        {
                            Console.WriteLine($"{url} 狀態碼：{(int)result.statusCode} ({result.statusCode})，耗時：{result.responseTimeMs} ms");

                            await _database.InsertResultAsync(
                                url,
                                (int)result.statusCode,
                                result.responseTimeMs,
                                DateTime.UtcNow
                            );
                        }
                        else
                        {
                            Console.WriteLine($"{url} 無法取得狀態碼。");
                        }
                    }
                }

                Console.WriteLine($"[{DateTime.UtcNow}] 檢查完成，30 秒後再次執行...");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);  // 測試用30秒執行一次
            }
        }

    }
}
