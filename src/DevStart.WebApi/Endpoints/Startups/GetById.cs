
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Startups.GetById;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.Startups
{
    internal sealed class GetById : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("startups/{startupId:guid}", async (
                Guid startupId, 
                IQueryHandler<GetStartupByIdQuery, StartupResponse> handler, 
                CancellationToken cancellationToken) =>
            {
                var query = new GetStartupByIdQuery(startupId);

                Result<StartupResponse> result = await handler.Handle(query, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .WithTags(Tags.Startups);
        }
    }
}
