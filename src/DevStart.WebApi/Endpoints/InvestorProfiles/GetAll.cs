using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.InvestorProfiles.GetAll;
using DevStart.Domain.Investors;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DevStart.WebApi.Endpoints.InvestorProfiles;

internal sealed class GetAll : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/investor-profiles", async (
            [FromQuery] int page,
            [FromQuery] int pageSize,
            IQueryHandler<GetInvestorProfilesQuery, List<InvestorCatalogResponse>> handler,
            CancellationToken cancellationToken,
            [FromQuery] InvestorProfileType? type = null,
            [FromQuery] InvestorSortBy sortBy = InvestorSortBy.DisplayName) =>
        {
            GetInvestorProfilesQuery query = new(page, pageSize, type, sortBy);
            Result<List<InvestorCatalogResponse>> result = await handler.Handle(query, cancellationToken);
            return result.Match(Results.Ok, CustomResults.Problem);
        })
            .WithTags(Tags.InvestorProfiles);
    }
}
