using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace DevStart.IntegrationTests.Infrastructure
{
    /// <summary>
    /// The TestServer has no real remote endpoint, so the "auth" rate limiter (which partitions by
    /// <c>RemoteIpAddress</c>) would lump every request from every test into one shared bucket and start
    /// returning 429s. This startup filter inserts a middleware at the very front of the pipeline that
    /// sets <c>Connection.RemoteIpAddress</c> from the <c>X-Test-Client-Ip</c> header, giving each test
    /// its own rate-limit partition. A dedicated rate-limiting test reuses one IP on purpose to trip 429.
    /// </summary>
    internal sealed class TestClientIpStartupFilter : IStartupFilter
    {
        public const string HeaderName = "X-Test-Client-Ip";

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) =>
            app =>
            {
                app.Use(async (context, nextMiddleware) =>
                {
                    if (context.Request.Headers.TryGetValue(HeaderName, out var value)
                        && IPAddress.TryParse(value.ToString(), out IPAddress? ip))
                    {
                        context.Connection.RemoteIpAddress = ip;
                    }

                    await nextMiddleware();
                });

                next(app);
            };
    }
}
