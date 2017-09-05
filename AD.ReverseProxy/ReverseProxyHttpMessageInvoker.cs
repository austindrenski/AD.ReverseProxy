using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace AD.ReverseProxy
{
    [PublicAPI]
    public class ReverseProxyHttpMessageInvoker : HttpMessageInvoker
    {
        private readonly HttpClient _client;

        public ReverseProxyHttpMessageInvoker() : this(new HttpClientHandler { UseDefaultCredentials = true, UseProxy = false })
        {
        }

        public ReverseProxyHttpMessageInvoker(HttpMessageHandler handler) : base(handler)
        {
            _client = new HttpClient(handler);
        }

        public override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
    }
}