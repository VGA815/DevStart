
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Profiles.Delete;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DevStart.WebApi.Endpoints.Profiles
{
    internal sealed class Delete : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete("profiles", async (
                [FromQuery] Guid profileId, 
                ICommandHandler<DeleteProfileCommand> handler, 
                CancellationToken cancellationToken) =>
            {
                var command = new DeleteProfileCommand(profileId);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(Results.NoContent, CustomResults.Problem);
            })
                .RequireAuthorization()
                .WithTags(Tags.Profiles);
        }
    }
}
