using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.ExpertExperiences.GetAllByExpertProfileId;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using System.Security.Claims;

namespace DevStart.WebApi.Endpoints.ExpertExperiences
{
    internal sealed class GetAllByExpertProfileId : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            // Anonymous for the same reason as the expert card itself: the experience list is what the
            // public card is mostly made of, and a logged-out visitor was getting an empty one. The
            // handler applies the card's visibility rule.
            app.MapGet("api/expert-profiles/{expertProfileId:guid}/experiences", async (
                Guid expertProfileId,
                ClaimsPrincipal user,
                IQueryHandler<GetExpertExperiencesByExpertProfileIdQuery, List<ExpertExperienceResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                Guid? viewerId = Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out Guid id)
                    ? id
                    : null;

                var query = new GetExpertExperiencesByExpertProfileIdQuery(expertProfileId, viewerId);
                Result<List<ExpertExperienceResponse>> result = await handler.Handle(query, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .WithTags(Tags.ExpertExperiences);
        }
    }
}
