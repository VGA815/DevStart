using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace DevStart.WebApi.Infrastructure
{
    internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            // Correlate the logged exception with the response the client sees.
            string traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

            logger.LogError(exception, "Unhandled exception occurred (traceId: {TraceId})", traceId);

            // If the response has already started, the status/headers are committed and we can't write a
            // ProblemDetails body — let the pipeline tear down the connection instead.
            if (httpContext.Response.HasStarted)
            {
                return false;
            }

            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1",
                Title = "Server failure",
                Instance = httpContext.Request.Path,
                Extensions = { ["traceId"] = traceId }
            };

            httpContext.Response.StatusCode = problemDetails.Status.Value;

            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            return true;
        }
    }
}
