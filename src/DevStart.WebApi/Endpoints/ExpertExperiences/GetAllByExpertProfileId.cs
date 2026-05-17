using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.ExpertExperiences.GetAllByExpertProfileId;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.ExpertExperiences
{
    internal sealed class GetAllByExpertProfileId : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/expert-profiles/{expertProfileId:guid}/experiences", async (
                Guid expertProfileId,
                IQueryHandler<GetExpertExperiencesByExpertProfileIdQuery, List<ExpertExperienceResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetExpertExperiencesByExpertProfileIdQuery(expertProfileId);
                Result<List<ExpertExperienceResponse>> result = await handler.Handle(query, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.ExpertExperiencesRead)
                .WithTags(Tags.ExpertExperiences);
        }
    }
}
