
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Profiles.GetById;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.Profiles
{
    internal sealed class GetById : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("profiles/{profileId:guid}", async (
                Guid profileId, 
                IQueryHandler<GetProfileByIdQuery, ProfileResponse> handler, 
                CancellationToken cancellationToken) =>
            {
                var query = new GetProfileByIdQuery(profileId);

                Result<ProfileResponse> result = await handler.Handle(query, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .WithTags(Tags.Profiles);
        }
    }
}
