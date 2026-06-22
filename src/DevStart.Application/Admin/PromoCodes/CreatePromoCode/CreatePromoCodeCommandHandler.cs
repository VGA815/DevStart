using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Admin;
using DevStart.Domain.PromoCodes;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Admin.PromoCodes.CreatePromoCode
{
    internal sealed class CreatePromoCodeCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IDateTimeProvider dateTimeProvider)
        : ICommandHandler<CreatePromoCodeCommand, Guid>
    {
        public async Task<Result<Guid>> Handle(CreatePromoCodeCommand command, CancellationToken cancellationToken)
        {
            string normalized = PromoCode.Normalize(command.Code);

            bool exists = await context.PromoCodes
                .AnyAsync(p => p.Code == normalized, cancellationToken);
            if (exists)
            {
                return Result.Failure<Guid>(PromoCodeErrors.CodeAlreadyExists);
            }

            DateTime now = dateTimeProvider.UtcNow;
            PromoCode promoCode = PromoCode.Create(
                command.Code,
                command.DiscountType,
                command.DiscountValue,
                command.FreePeriodDays,
                command.Plan,
                command.MaxRedemptions,
                command.ValidFrom,
                command.ValidUntil,
                userContext.UserId,
                now);

            context.PromoCodes.Add(promoCode);
            context.AdminActionLogs.Add(AdminActionLog.Create(
                userContext.UserId,
                AdminActionType.CreatePromoCode,
                AdminTargetType.PromoCode,
                promoCode.Id,
                $"Created promo code {promoCode.Code}",
                now));

            await context.SaveChangesAsync(cancellationToken);
            return promoCode.Id;
        }
    }
}
