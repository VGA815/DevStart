using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.AccountDeletion.GetStatus;
using DevStart.Application.AccountDeletion.RequestDeletion;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DevStart.WebApi.Endpoints.Users
{
    internal sealed class RequestAccountDeletion : IEndpoint
    {
        /// <summary>Password is required for accounts that have one; OAuth-only accounts send nothing.</summary>
        public sealed record Request(string? Password);

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/users/me/deletion", async (
                [FromBody] Request? request,
                ICommandHandler<RequestAccountDeletionCommand, AccountDeletionStatusResponse> handler,
                CancellationToken cancellationToken) =>
            {
                Result<AccountDeletionStatusResponse> result = await handler.Handle(
                    new RequestAccountDeletionCommand(request?.Password), cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .WithTags(Tags.AccountDeletion)
                .RequireAuthorization()
                .RequireRateLimiting("per-user");
        }
    }
}
