using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Admin.Users.BanUser;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DevStart.WebApi.Endpoints.Admin.Users
{
    internal sealed class BanUser : IEndpoint
    {
        public sealed record Request(string Reason, DateTime? ExpiresAt);

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/admin/users/{id:guid}/ban", async (
                Guid id,
                [FromBody] Request request,
                ICommandHandler<BanUserCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new BanUserCommand(id, request.Reason, request.ExpiresAt);
                Result result = await handler.Handle(command, cancellationToken);
                return result.Match(Results.NoContent, CustomResults.Problem);
            })
                .HasPermission(Permissions.AdminUsersBan)
                .WithTags(Tags.Admin);
        }
    }
}
