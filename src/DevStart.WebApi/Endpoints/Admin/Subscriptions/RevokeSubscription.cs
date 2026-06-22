using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Admin.Subscriptions.RevokeSubscription;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DevStart.WebApi.Endpoints.Admin.Subscriptions
{
    internal sealed class RevokeSubscription : IEndpoint
    {
        public sealed record Request(string Reason);

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/admin/subscriptions/{id:guid}/revoke", async (
                Guid id,
                [FromBody] Request request,
                ICommandHandler<RevokeSubscriptionCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new RevokeSubscriptionCommand(id, request.Reason);
                Result result = await handler.Handle(command, cancellationToken);
                return result.Match(Results.NoContent, CustomResults.Problem);
            })
                .HasPermission(Permissions.AdminSubscriptionsManage)
                .WithTags(Tags.Admin);
        }
    }
}
