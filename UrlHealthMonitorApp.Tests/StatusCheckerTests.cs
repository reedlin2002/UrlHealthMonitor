using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

// 命名空間可依你的專案名調整
namespace UrlHealthMonitorApp.Tests
{
    public class StatusCheckerTests
    {
        [Fact]
        public async Task GetStatusCodeAsync_ReturnsStatusCodeAndResponseTime()
        {
            // Arrange
            var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK);
            var fakeHandler = new FakeHttpMessageHandler(fakeResponse);
            var httpClient = new HttpClient(fakeHandler);

            var checker = new StatusChecker(httpClient);

            // Act
            var result = await checker.GetStatusCodeAsync("https://example.com");

            // Assert
            Assert.Equal(HttpStatusCode.OK, result.statusCode);
            Assert.True(result.responseTimeMs >= 0);
        }
    }

    // 自訂 Fake Handler，模擬 HttpClient
    public class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public FakeHttpMessageHandler(HttpResponseMessage response)
        {
            _response = response;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_response);
        }
    }
}
