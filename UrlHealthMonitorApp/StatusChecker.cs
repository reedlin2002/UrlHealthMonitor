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

        public StatusChecker(HttpClient? client = null)
        {
            _client = client ?? new HttpClient();
        }

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
                return (null, 0);
            }
        }
    }
}
