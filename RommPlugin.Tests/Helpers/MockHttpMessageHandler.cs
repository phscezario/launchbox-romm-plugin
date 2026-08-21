using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace RommPlugin.Tests.Helpers
{
    public class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;
        public List<HttpRequestMessage> Requests { get; } = new List<HttpRequestMessage>();

        public MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        public MockHttpMessageHandler(HttpResponseMessage response)
        {
            _handler = (req, ct) => Task.FromResult(response);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return await _handler(request, cancellationToken);
        }
    }
}
