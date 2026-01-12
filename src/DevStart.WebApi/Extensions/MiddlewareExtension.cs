using DevStart.WebApi.Middleware;

namespace DevStart.WebApi.Extensions
{
    public static class MiddlewareExtension
    {
        public static IApplicationBuilder UseRequestContextLogging(this IApplicationBuilder app)
        {
            app.UseMiddleware<RequestContextLoggingMiddleware>();

            return app;
        }
    }
}
