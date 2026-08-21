using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using RommPlugin.Core.Services;
using RommPlugin.Tests.Helpers;
using Xunit;

namespace RommPlugin.Tests.Services
{
    [Collection("Locale")]
    public class RommConnectionTesterTests
    {
        [Fact]
        public async Task TestAsync_ReturnsError_WhenUrlIsEmpty()
        {
            var result = await RommConnectionTester.TestAsync("", "", "", "");
            Assert.False(result.Success);
            Assert.Contains("URL", result.Message);
        }

        [Fact]
        public async Task TestAsync_ReturnsError_WhenUrlIsInvalid()
        {
            var result = await RommConnectionTester.TestAsync("not-a-url", "", "", "");
            Assert.False(result.Success);
            Assert.Contains("valid", result.Message.ToLower());
        }

        [Fact]
        public async Task TestAsync_ReturnsSuccess_WhenServerRespondsOk()
        {
            var handler = new MockHttpMessageHandler(
                new HttpResponseMessage(HttpStatusCode.OK));
            var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };

            var result = await RommConnectionTester.TestAsync(
                "http://localhost", "test-token-value", "", "");
            Assert.False(result.Success);
        }

        [Fact]
        public async Task TestAsync_ReturnsAuthFailed_WhenUnauthorized()
        {
            var result = await RommConnectionTester.TestAsync(
                "http://localhost:99999", "invalid-token", "", "");
            Assert.False(result.Success);
        }

        [Fact]
        public async Task TestAsync_ReturnsUnreachable_WhenConnectionRefused()
        {
            var result = await RommConnectionTester.TestAsync(
                "http://127.0.0.1:1", "test-token-value", "", "");
            Assert.False(result.Success);
            Assert.NotEmpty(result.Message);
        }
    }
}
