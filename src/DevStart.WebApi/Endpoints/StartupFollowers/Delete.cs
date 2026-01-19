
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.StartupFollowers.Delete;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DevStart.WebApi.Endpoints.StartupFollowers
{
    internal sealed class Delete : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete("startups/followers", async (
                [FromQuery] Guid startupId, 
                ICommandHandler<DeleteStartupFollowerCommand> handler, 
                CancellationToken cancellationToken) =>
            {
                var command = new DeleteStartupFollowerCommand(startupId);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(Results.NoContent, CustomResults.Problem);
            })
                .RequireAuthorization()
                .WithTags(Tags.StartupFollowers);
        }
    }
}
