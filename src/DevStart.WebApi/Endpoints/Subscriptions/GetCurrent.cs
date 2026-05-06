using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Subscriptions.GetCurrent;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.Subscriptions
{
    internal sealed class GetCurrent : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/subscriptions/current", async (
                IQueryHandler<GetCurrentSubscriptionQuery, CurrentSubscriptionResponse> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetCurrentSubscriptionQuery();
                Result<CurrentSubscriptionResponse> result = await handler.Handle(query, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.SubscriptionsRead)
                .WithTags(Tags.Subscriptions);
        }
    }
}
