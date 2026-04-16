using Microsoft.AspNetCore.Mvc;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.InviteTokens.Use;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.InviteTokens
{
    internal sealed class Use : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/organizations/join", async (
                ICommandHandler<UseInviteTokenCommand, Guid> handler,
                CancellationToken cancellationToken,
                [FromQuery] Guid tokenId) =>
            {
                UseInviteTokenCommand useInviteTokenCommand = new(tokenId);

                Result<Guid> result = await handler.Handle(useInviteTokenCommand, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .RequireAuthorization()
                .WithTags(Tags.InviteTokens);
        }
    }
}
