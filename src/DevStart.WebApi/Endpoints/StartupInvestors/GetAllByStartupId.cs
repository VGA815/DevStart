
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.StartupInvestors.GetAllByStartupId;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.StartupInvestors
{
    internal sealed class GetAllByStartupId : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("startups/{startupId:guid}/investors", async (
                Guid startupId, 
                IQueryHandler<GetStartupInvestorsByStartupIdQuery, List<StartupInvestorResponse>> handler, 
                CancellationToken cancellationToken) =>
            {
                var query = new GetStartupInvestorsByStartupIdQuery(startupId);

                Result<List<StartupInvestorResponse>> result = await handler.Handle(query, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .WithTags(Tags.StartupInvestors);
        }
    }
}
