using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.InvestmentDeals.GetAllByStartupId;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.InvestmentDeals
{
    internal sealed class GetAllByStartupId : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/startups/{startupId:guid}/investment-deals", async (
                Guid startupId,
                IQueryHandler<GetInvestmentDealsByStartupIdQuery, List<InvestmentDealResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetInvestmentDealsByStartupIdQuery(startupId);
                Result<List<InvestmentDealResponse>> result = await handler.Handle(query, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.InvestmentDealsRead)
                .WithTags(Tags.InvestmentDeals);
        }
    }
}
