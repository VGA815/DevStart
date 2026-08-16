using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.AccountDeletion.GetStatus;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.Users
{
    internal sealed class AccountDeletionStatus : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/users/me/deletion", async (
                IQueryHandler<GetAccountDeletionStatusQuery, AccountDeletionStatusResponse> handler,
                CancellationToken cancellationToken) =>
            {
                Result<AccountDeletionStatusResponse> result =
                    await handler.Handle(new GetAccountDeletionStatusQuery(), cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .WithTags(Tags.AccountDeletion)
                .RequireAuthorization()
                .RequireRateLimiting("per-user");
        }
    }
}
