using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Admin.Startups.UnbanStartup;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DevStart.WebApi.Endpoints.Admin.Startups
{
    internal sealed class UnbanStartup : IEndpoint
    {
        public sealed record Request(string? Reason);

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/admin/startups/{id:guid}/unban", async (
                Guid id,
                ICommandHandler<UnbanStartupCommand> handler,
                CancellationToken cancellationToken,
                [FromBody] Request? request = null) =>
            {
                var command = new UnbanStartupCommand(id, request?.Reason);
                Result result = await handler.Handle(command, cancellationToken);
                return result.Match(Results.NoContent, CustomResults.Problem);
            })
                .HasPermission(Permissions.AdminStartupsBan)
                .WithTags(Tags.Admin);
        }
    }
}
