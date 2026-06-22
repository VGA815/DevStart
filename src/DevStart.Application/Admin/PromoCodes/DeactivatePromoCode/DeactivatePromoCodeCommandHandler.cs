using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Admin;
using DevStart.Domain.PromoCodes;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Admin.PromoCodes.DeactivatePromoCode
{
    internal sealed class DeactivatePromoCodeCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IDateTimeProvider dateTimeProvider)
        : ICommandHandler<DeactivatePromoCodeCommand>
    {
        public async Task<Result> Handle(DeactivatePromoCodeCommand command, CancellationToken cancellationToken)
        {
            PromoCode? promoCode = await context.PromoCodes
                .SingleOrDefaultAsync(p => p.Id == command.PromoCodeId, cancellationToken);
            if (promoCode is null)
            {
                return Result.Failure(PromoCodeErrors.NotFound(command.PromoCodeId));
            }

            promoCode.Deactivate();

            DateTime now = dateTimeProvider.UtcNow;
            context.AdminActionLogs.Add(AdminActionLog.Create(
                userContext.UserId,
                AdminActionType.DeactivatePromoCode,
                AdminTargetType.PromoCode,
                promoCode.Id,
                $"Deactivated promo code {promoCode.Code}",
                now));

            await context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }
}
