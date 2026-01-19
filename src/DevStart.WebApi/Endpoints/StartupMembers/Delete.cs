
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.StartupMembers.Delete;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DevStart.WebApi.Endpoints.StartupMembers
{
    internal sealed class Delete : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete("startups/members", async (
                [FromQuery] Guid startupId,
                [FromQuery] Guid profileId, 
                ICommandHandler<DeleteStartupMemberCommand> handler, 
                CancellationToken cancellationToken) =>
            {
                var command = new DeleteStartupMemberCommand(startupId, profileId);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(Results.NoContent, CustomResults.Problem);
            })
                .RequireAuthorization()
                .WithTags(Tags.StartupMembers);
        }
    }
}
