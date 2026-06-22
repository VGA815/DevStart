using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Admin.PromoCodes.CreatePromoCode;
using DevStart.Domain.PromoCodes;
using DevStart.Domain.Subscriptions;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DevStart.WebApi.Endpoints.Admin.PromoCodes
{
    internal sealed class CreatePromoCode : IEndpoint
    {
        public sealed record Request(
            string Code,
            PromoDiscountType DiscountType,
            decimal DiscountValue,
            int? FreePeriodDays,
            SubscriptionPlan Plan,
            int? MaxRedemptions,
            DateTime? ValidFrom,
            DateTime? ValidUntil);

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/admin/promo-codes", async (
                [FromBody] Request request,
                ICommandHandler<CreatePromoCodeCommand, Guid> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new CreatePromoCodeCommand(
                    request.Code,
                    request.DiscountType,
                    request.DiscountValue,
                    request.FreePeriodDays,
                    request.Plan,
                    request.MaxRedemptions,
                    request.ValidFrom,
                    request.ValidUntil);
                Result<Guid> result = await handler.Handle(command, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.AdminPromoCodesManage)
                .WithTags(Tags.Admin);
        }
    }
}
