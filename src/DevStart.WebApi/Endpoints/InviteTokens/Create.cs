using Microsoft.AspNetCore.Mvc;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.InviteTokens.Create;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.InviteTokens
{
    internal sealed class Create : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/invite-tokens/", async (
                ICommandHandler<CreateInviteTokenCommand, Guid> handler,
                CancellationToken cancellationToken,
                [FromQuery] Guid startupId) =>
            {
                CreateInviteTokenCommand command = new(startupId);

                Result<Guid> result = await handler.Handle(command, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .RequireAuthorization()
                .WithTags(Tags.InviteTokens);
        }
    }
}
