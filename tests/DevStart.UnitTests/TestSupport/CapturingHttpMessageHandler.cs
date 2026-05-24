using System.Net;

namespace DevStart.UnitTests.TestSupport
{
    internal sealed class CapturingHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public string? LastRequestBody { get; private set; }
        public HttpRequestMessage? LastRequest { get; private set; }

        public CapturingHttpMessageHandler(HttpStatusCode status, string json)
            : this(_ => new HttpResponseMessage(status)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
            })
        {
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            if (request.Content is not null)
            {
                LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
            }
            return responder(request);
        }
    }
}
