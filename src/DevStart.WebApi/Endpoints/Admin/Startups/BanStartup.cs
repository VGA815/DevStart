using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Admin.Startups.BanStartup;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DevStart.WebApi.Endpoints.Admin.Startups
{
    internal sealed class BanStartup : IEndpoint
    {
        public sealed record Request(string Reason, DateTime? ExpiresAt);

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/admin/startups/{id:guid}/ban", async (
                Guid id,
                [FromBody] Request request,
                ICommandHandler<BanStartupCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new BanStartupCommand(id, request.Reason, request.ExpiresAt);
                Result result = await handler.Handle(command, cancellationToken);
                return result.Match(Results.NoContent, CustomResults.Problem);
            })
                .HasPermission(Permissions.AdminStartupsBan)
                .WithTags(Tags.Admin);
        }
    }
}
