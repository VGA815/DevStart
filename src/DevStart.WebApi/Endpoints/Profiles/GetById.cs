
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Profiles.GetById;
using DevStart.Application.Profiles.IncrementViews;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using System.Security.Claims;

namespace DevStart.WebApi.Endpoints.Profiles
{
    internal sealed class GetById : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/profiles/{profileId:guid}", async (
                Guid profileId,
                ClaimsPrincipal user,
                IQueryHandler<GetProfileByIdQuery, ProfileResponse> handler,
                ICommandHandler<IncrementProfileViewsCommand> incrementHandler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetProfileByIdQuery(profileId);

                Result<ProfileResponse> result = await handler.Handle(query, cancellationToken);

                // Count a profile view only when the requester is not the owner. The endpoint is
                // anonymous, so an unauthenticated viewer (no user id) also counts.
                if (result.IsSuccess)
                {
                    Guid? viewerId = Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out Guid id)
                        ? id
                        : null;

                    if (viewerId != result.Value.UserId)
                    {
                        await incrementHandler.Handle(new IncrementProfileViewsCommand(profileId), cancellationToken);
                    }
                }

                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .WithTags(Tags.Profiles);
        }
    }
}
