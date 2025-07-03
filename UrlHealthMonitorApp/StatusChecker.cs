using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace UrlHealthMonitorApp
{
    public class StatusChecker
    {
        private readonly HttpClient _client;

        // 建構式：注入 HttpClient（方便測試時提供假的）
        public StatusChecker(HttpClient? client = null)
        {
            _client = client ?? new HttpClient();
        }

        // 非同步檢查
        public async Task<(HttpStatusCode? statusCode, long responseTimeMs)> GetStatusCodeAsync(string url)
        {
            try
            {
                var watch = Stopwatch.StartNew();
                var response = await _client.GetAsync(url);
                watch.Stop();

                return (response.StatusCode, watch.ElapsedMilliseconds);
            }
            catch (HttpRequestException)
            {
                // 網路錯誤回傳 null
                return (null, 0);
            }
        }
    }
}
