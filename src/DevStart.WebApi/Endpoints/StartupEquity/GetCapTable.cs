using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.StartupEquity.GetCapTable;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.StartupEquity
{
    internal sealed class GetCapTable : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/startups/{startupId:guid}/cap-table", async (
                Guid startupId,
                IQueryHandler<GetStartupCapTableQuery, StartupCapTableResponse> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetStartupCapTableQuery(startupId);

                Result<StartupCapTableResponse> result = await handler.Handle(query, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.StartupEquityRead)
                .WithTags(Tags.StartupEquity);
        }
    }
}
