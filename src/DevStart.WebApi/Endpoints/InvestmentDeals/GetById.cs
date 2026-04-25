using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.InvestmentDeals.GetById;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.InvestmentDeals
{
    internal sealed class GetById : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/investment-deals/{dealId:guid}", async (
                Guid dealId,
                IQueryHandler<GetInvestmentDealByIdQuery, InvestmentDealResponse> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetInvestmentDealByIdQuery(dealId);
                Result<InvestmentDealResponse> result = await handler.Handle(query, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.InvestmentDealsRead)
                .WithTags(Tags.InvestmentDeals);
        }
    }
}
