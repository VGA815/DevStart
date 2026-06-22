using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Subscriptions.Checkout;
using DevStart.Domain.Subscriptions;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DevStart.WebApi.Endpoints.Subscriptions
{
    internal sealed class Checkout : IEndpoint
    {
        public sealed record Request(string? PromoCode);

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/subscriptions/checkout", async (
                ICommandHandler<CreateCheckoutCommand, CheckoutResponse> handler,
                CancellationToken cancellationToken,
                [FromBody] Request? request = null) =>
            {
                var command = new CreateCheckoutCommand(SubscriptionPlan.Pro, request?.PromoCode);
                Result<CheckoutResponse> result = await handler.Handle(command, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.SubscriptionsCheckout)
                .WithTags(Tags.Subscriptions);
        }
    }
}
