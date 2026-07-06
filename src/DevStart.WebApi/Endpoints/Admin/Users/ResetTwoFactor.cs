using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Admin.Users.ResetTwoFactor;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DevStart.WebApi.Endpoints.Admin.Users
{
    internal sealed class ResetTwoFactor : IEndpoint
    {
        public sealed record Request(string Reason);

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/admin/users/{id:guid}/2fa/reset", async (
                Guid id,
                [FromBody] Request request,
                ICommandHandler<ResetUserTwoFactorCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new ResetUserTwoFactorCommand(id, request.Reason);
                Result result = await handler.Handle(command, cancellationToken);
                return result.Match(Results.NoContent, CustomResults.Problem);
            })
                .HasPermission(Permissions.AdminUsersTwoFactorReset)
                .WithTags(Tags.Admin);
        }
    }
}
