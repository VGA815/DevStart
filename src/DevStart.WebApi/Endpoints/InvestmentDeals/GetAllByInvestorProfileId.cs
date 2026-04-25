using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.InvestmentDeals.GetAllByInvestorProfileId;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.InvestmentDeals
{
    internal sealed class GetAllByInvestorProfileId : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/investor-profiles/{userId:guid}/investment-deals", async (
                Guid userId,
                IQueryHandler<GetInvestmentDealsByInvestorProfileIdQuery, List<InvestmentDealResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetInvestmentDealsByInvestorProfileIdQuery(userId);
                Result<List<InvestmentDealResponse>> result = await handler.Handle(query, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.InvestmentDealsRead)
                .WithTags(Tags.InvestmentDeals);
        }
    }
}
