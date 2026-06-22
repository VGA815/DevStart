using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Admin.Subscriptions.GetUserPayments;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.Admin.Subscriptions
{
    internal sealed class GetUserPayments : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/admin/users/{id:guid}/payments", async (
                Guid id,
                IQueryHandler<GetUserPaymentsForAdminQuery, List<AdminPaymentResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetUserPaymentsForAdminQuery(id);
                Result<List<AdminPaymentResponse>> result = await handler.Handle(query, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.AdminSubscriptionsRead)
                .WithTags(Tags.Admin);
        }
    }
}
