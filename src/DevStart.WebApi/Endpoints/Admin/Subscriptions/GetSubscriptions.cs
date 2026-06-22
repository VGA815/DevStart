using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Admin.Subscriptions.GetSubscriptions;
using DevStart.Domain.Subscriptions;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DevStart.WebApi.Endpoints.Admin.Subscriptions
{
    internal sealed class GetSubscriptions : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/admin/subscriptions", async (
                IQueryHandler<GetAdminSubscriptionsQuery, List<AdminSubscriptionResponse>> handler,
                CancellationToken cancellationToken,
                [FromQuery] Guid? userId = null,
                [FromQuery] SubscriptionStatus? status = null,
                [FromQuery] SubscriptionPlan? plan = null,
                [FromQuery] int pageNumber = 1,
                [FromQuery] int pageSize = 50) =>
            {
                var query = new GetAdminSubscriptionsQuery(userId, status, plan, pageNumber, pageSize);
                Result<List<AdminSubscriptionResponse>> result = await handler.Handle(query, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.AdminSubscriptionsRead)
                .WithTags(Tags.Admin);
        }
    }
}
