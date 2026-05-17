using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.ExpertProfiles.GetById;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.ExpertProfiles
{
    internal sealed class GetById : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/expert-profiles/{userId:guid}", async (
                Guid userId,
                IQueryHandler<GetExpertProfileByIdQuery, ExpertProfileResponse> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetExpertProfileByIdQuery(userId);
                Result<ExpertProfileResponse> result = await handler.Handle(query, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.ExpertProfilesRead)
                .WithTags(Tags.ExpertProfiles);
        }
    }
}
