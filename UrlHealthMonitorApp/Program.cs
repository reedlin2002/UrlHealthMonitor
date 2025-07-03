using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace UrlHealthMonitorApp
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            string input;

            // 優先使用命令列參數
            if (args.Length > 0)
            {
                input = string.Join(",", args); // 多個參數用逗號合併
            }
            else
            {
                // 次要使用環境變數 URLS
                input = Environment.GetEnvironmentVariable("URLS") ?? "";
            }

            // 如果仍然沒資料，進入互動輸入
            if (string.IsNullOrWhiteSpace(input))
            {
                Console.WriteLine("請輸入要檢查的網址（多個網址請用逗號分隔）：");
                input = Console.ReadLine() ?? "";
            }

            if (string.IsNullOrWhiteSpace(input))
            {
                Console.WriteLine("沒有輸入任何網址，結束程式。");
                return;
            }

            var urls = input.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(u => u.Trim())
                            .ToList();

            var checker = new StatusChecker();

            var dbPath = Environment.GetEnvironmentVariable("DATABASE_PATH") ?? "results.db";
            var db = new Database(dbPath);

            foreach (var url in urls)
            {
                var result = await checker.GetStatusCodeAsync(url);

                if (result.statusCode.HasValue)
                {
                    Console.WriteLine($"{url} 狀態碼：{(int)result.statusCode} ({result.statusCode})，耗時：{result.responseTimeMs} ms");

                    await db.InsertResultAsync(
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

            Console.WriteLine("已完成所有檢查。");
        }
    }
}
