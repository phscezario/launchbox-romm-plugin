using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using RommPlugin.Core.Services;
using RommPlugin.Tests.Helpers;
using Xunit;

namespace RommPlugin.Tests.Services
{
    [Collection("Locale")]
    public class RommConnectionTesterTests
    {
        public RommConnectionTesterTests()
        {
            LocaleFixture.EnsureInitialized();
        }

        [Fact]
        public async Task TestAsync_ReturnsError_WhenUrlIsEmpty()
        {
            var tester = new RommConnectionTester();
            var result = await tester.TestAsync("", "", "", "");
            Assert.False(result.Success);
            Assert.Contains("URL", result.Message);
        }

        [Fact]
        public async Task TestAsync_ReturnsError_WhenUrlIsInvalid()
        {
            var tester = new RommConnectionTester();
            var result = await tester.TestAsync("not-a-url", "", "", "");
            Assert.False(result.Success);
            Assert.Contains("valid", result.Message.ToLower());
        }

        [Fact]
        public async Task TestAsync_ReturnsSuccess_WhenServerRespondsOk()
        {
            var handler = new MockHttpMessageHandler(
                new HttpResponseMessage(HttpStatusCode.OK));
            var tester = new RommConnectionTester(new HttpClient(handler));

            var result = await tester.TestAsync(
                "http://localhost", "test-token-value", "", "");

            Assert.True(result.Success);
        }

        [Fact]
        public async Task TestAsync_ReturnsAuthFailed_WhenUnauthorized()
        {
            var handler = new MockHttpMessageHandler(
                new HttpResponseMessage(HttpStatusCode.Unauthorized));
            var tester = new RommConnectionTester(new HttpClient(handler));

            var result = await tester.TestAsync(
                "http://localhost", "invalid-token", "", "");

            Assert.False(result.Success);
        }

        [Fact]
        public async Task TestAsync_ReturnsAuthFailed_WhenForbidden()
        {
            var handler = new MockHttpMessageHandler(
                new HttpResponseMessage(HttpStatusCode.Forbidden));
            var tester = new RommConnectionTester(new HttpClient(handler));

            var result = await tester.TestAsync(
                "http://localhost", "invalid-token", "", "");

            Assert.False(result.Success);
        }

        [Fact]
        public async Task TestAsync_ReturnsServerError_When500()
        {
            var handler = new MockHttpMessageHandler(
                new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    ReasonPhrase = "Internal Server Error"
                });
            var tester = new RommConnectionTester(new HttpClient(handler));

            var result = await tester.TestAsync(
                "http://localhost", "test-token", "", "");

            Assert.False(result.Success);
            Assert.Contains("500", result.Message);
        }

        [Fact]
        public async Task TestAsync_ReturnsUnreachable_WhenConnectionRefused()
        {
            var handler = new MockHttpMessageHandler(
                new Func<HttpRequestMessage, System.Threading.CancellationToken,
                    Task<HttpResponseMessage>>((req, ct) =>
                    throw new HttpRequestException("Connection refused")));
            var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:1") };
            var tester = new RommConnectionTester(client);

            var result = await tester.TestAsync(
                "http://localhost:1", "test-token-value", "", "");

            Assert.False(result.Success);
            Assert.NotEmpty(result.Message);
        }

        [Fact]
        public async Task TestAsync_UsesBearerAuth_WhenTokenProvided()
        {
            var handler = new MockHttpMessageHandler(
                new HttpResponseMessage(HttpStatusCode.OK));
            var client = new HttpClient(handler);
            var tester = new RommConnectionTester(client);

            var result = await tester.TestAsync("http://localhost", "my-token", "", "");

            Assert.True(result.Success);
            Assert.Single(handler.Requests);
            Assert.Equal("Bearer", handler.Requests[0].Headers.Authorization.Scheme);
            Assert.Equal("my-token", handler.Requests[0].Headers.Authorization.Parameter);
        }

        [Fact]
        public async Task TestAsync_ReturnsSuccess_WhenUrlHasTrailingSlash()
        {
            var handler = new MockHttpMessageHandler(
                new HttpResponseMessage(HttpStatusCode.OK));
            var tester = new RommConnectionTester(new HttpClient(handler));

            var result = await tester.TestAsync(
                "http://localhost/", "test-token", "", "");

            Assert.True(result.Success);
        }
    }
}
