using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Admin.Subscriptions.ExtendSubscription;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DevStart.WebApi.Endpoints.Admin.Subscriptions
{
    internal sealed class ExtendSubscription : IEndpoint
    {
        public sealed record Request(int AdditionalDays, string Reason);

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/admin/subscriptions/{id:guid}/extend", async (
                Guid id,
                [FromBody] Request request,
                ICommandHandler<ExtendSubscriptionCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new ExtendSubscriptionCommand(id, request.AdditionalDays, request.Reason);
                Result result = await handler.Handle(command, cancellationToken);
                return result.Match(Results.NoContent, CustomResults.Problem);
            })
                .HasPermission(Permissions.AdminSubscriptionsManage)
                .WithTags(Tags.Admin);
        }
    }
}
