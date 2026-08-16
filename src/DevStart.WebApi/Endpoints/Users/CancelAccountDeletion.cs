using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.AccountDeletion.CancelDeletion;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.Users
{
    internal sealed class CancelAccountDeletion : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            // DELETE on the request resource, not on the account: this calls off a scheduled erasure.
            app.MapDelete("api/users/me/deletion", async (
                ICommandHandler<CancelAccountDeletionCommand> handler,
                CancellationToken cancellationToken) =>
            {
                Result result = await handler.Handle(new CancelAccountDeletionCommand(), cancellationToken);

                return result.Match(Results.NoContent, CustomResults.Problem);
            })
                .WithTags(Tags.AccountDeletion)
                .RequireAuthorization()
                .RequireRateLimiting("per-user");
        }
    }
}
