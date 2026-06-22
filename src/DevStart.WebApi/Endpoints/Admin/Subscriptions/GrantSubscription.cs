using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Admin.Subscriptions.GrantSubscription;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DevStart.WebApi.Endpoints.Admin.Subscriptions
{
    internal sealed class GrantSubscription : IEndpoint
    {
        public sealed record Request(Guid UserId, int? DurationDays, string Reason);

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/admin/subscriptions/grant", async (
                [FromBody] Request request,
                ICommandHandler<GrantSubscriptionCommand, Guid> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new GrantSubscriptionCommand(request.UserId, request.DurationDays, request.Reason);
                Result<Guid> result = await handler.Handle(command, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.AdminSubscriptionsManage)
                .WithTags(Tags.Admin);
        }
    }
}
