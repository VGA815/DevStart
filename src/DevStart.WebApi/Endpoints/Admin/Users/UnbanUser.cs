using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Admin.Users.UnbanUser;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DevStart.WebApi.Endpoints.Admin.Users
{
    internal sealed class UnbanUser : IEndpoint
    {
        public sealed record Request(string? Reason);

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/admin/users/{id:guid}/unban", async (
                Guid id,
                ICommandHandler<UnbanUserCommand> handler,
                CancellationToken cancellationToken,
                [FromBody] Request? request = null) =>
            {
                var command = new UnbanUserCommand(id, request?.Reason);
                Result result = await handler.Handle(command, cancellationToken);
                return result.Match(Results.NoContent, CustomResults.Problem);
            })
                .HasPermission(Permissions.AdminUsersBan)
                .WithTags(Tags.Admin);
        }
    }
}
