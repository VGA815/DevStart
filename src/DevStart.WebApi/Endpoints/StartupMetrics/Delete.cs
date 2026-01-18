
namespace DevStart.WebApi.Endpoints.StartupMetrics
{
    internal sealed class Delete : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete("startups/metrics", async () => { });
        }
    }
}
