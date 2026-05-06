using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Subscriptions.Checkout;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.Subscriptions
{
    internal sealed class Checkout : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/subscriptions/checkout", async (
                ICommandHandler<CreateCheckoutCommand, CheckoutResponse> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new CreateCheckoutCommand();
                Result<CheckoutResponse> result = await handler.Handle(command, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.SubscriptionsCheckout)
                .WithTags(Tags.Subscriptions);
        }
    }
}
