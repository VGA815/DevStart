using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Subscriptions.GetPayments;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.Subscriptions
{
    internal sealed class GetPayments : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/subscriptions/payments", async (
                IQueryHandler<GetUserPaymentsQuery, List<PaymentHistoryResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetUserPaymentsQuery();
                Result<List<PaymentHistoryResponse>> result = await handler.Handle(query, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.SubscriptionsRead)
                .WithTags(Tags.Subscriptions);
        }
    }
}
