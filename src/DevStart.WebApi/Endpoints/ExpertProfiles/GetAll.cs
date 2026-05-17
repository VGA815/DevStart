using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.ExpertProfiles.GetAll;
using DevStart.Domain.Experts;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DevStart.WebApi.Endpoints.ExpertProfiles;

internal sealed class GetAll : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/expert-profiles", async (
            [FromQuery] int page,
            [FromQuery] int pageSize,
            IQueryHandler<GetExpertProfilesQuery, List<ExpertCatalogResponse>> handler,
            CancellationToken cancellationToken,
            [FromQuery] ExpertSpecialization? specialization = null,
            [FromQuery] ExpertSortBy sortBy = ExpertSortBy.DisplayName) =>
        {
            GetExpertProfilesQuery query = new(page, pageSize, specialization, sortBy);
            Result<List<ExpertCatalogResponse>> result = await handler.Handle(query, cancellationToken);
            return result.Match(Results.Ok, CustomResults.Problem);
        })
            .WithTags(Tags.ExpertProfiles);
    }
}
