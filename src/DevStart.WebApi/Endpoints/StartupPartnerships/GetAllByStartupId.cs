using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.StartupPartnerships.GetAllByStartupId;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.StartupPartnerships
{
    /// <summary>
    /// The startup's strategic partnerships: who, what kind of arrangement, and what it gives them.
    /// The whole list ships, worked-out records and placeholders alike — the reader seeing that eight
    /// records carry no account of the partnership is the point, and it is what makes the graded
    /// Berkus factor legible (М3).
    /// </summary>
    internal sealed class GetAllByStartupId : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/startups/{startupId:guid}/partnerships", async (
                Guid startupId,
                IQueryHandler<GetStartupPartnershipsByStartupIdQuery, List<StartupPartnershipResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetStartupPartnershipsByStartupIdQuery(startupId);
                Result<List<StartupPartnershipResponse>> result = await handler.Handle(query, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.StartupPartnershipsRead)
                .WithTags(Tags.StartupPartnerships);
        }
    }
}
