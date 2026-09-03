using System;
using System.Net.Http;
using System.Text;
using RommPlugin.Core.Helpers;
using Xunit;

namespace RommPlugin.Tests.Helpers
{
    public class AuthHeaderHelperTests
    {
        [Fact]
        public void ApplyAuthentication_SetsBearer_WhenTokenProvided()
        {
            var http = new HttpClient();

            AuthHeaderHelper.ApplyAuthentication(http, "my-api-token", "", "");

            Assert.Equal("Bearer", http.DefaultRequestHeaders.Authorization.Scheme);
            Assert.Equal("my-api-token", http.DefaultRequestHeaders.Authorization.Parameter);
        }

        [Fact]
        public void ApplyAuthentication_SetsBasic_WhenTokenEmpty()
        {
            var http = new HttpClient();

            AuthHeaderHelper.ApplyAuthentication(http, "", "admin", "secret");

            Assert.Equal("Basic", http.DefaultRequestHeaders.Authorization.Scheme);
            var decoded = Encoding.UTF8.GetString(
                Convert.FromBase64String(http.DefaultRequestHeaders.Authorization.Parameter));
            Assert.Equal("admin:secret", decoded);
        }

        [Fact]
        public void ApplyAuthentication_PrefersBearerOverBasic()
        {
            var http = new HttpClient();

            AuthHeaderHelper.ApplyAuthentication(http, "token", "user", "pass");

            Assert.Equal("Bearer", http.DefaultRequestHeaders.Authorization.Scheme);
        }

        [Fact]
        public void ApplyAuthentication_DoesNothing_WhenAllEmpty()
        {
            var http = new HttpClient();

            AuthHeaderHelper.ApplyAuthentication(http, "", "", "");

            Assert.Null(http.DefaultRequestHeaders.Authorization);
        }

        [Fact]
        public void ApplyAuthentication_DoesNothing_WhenAllNull()
        {
            var http = new HttpClient();

            AuthHeaderHelper.ApplyAuthentication(http, null, null, null);

            Assert.Null(http.DefaultRequestHeaders.Authorization);
        }
    }
}
