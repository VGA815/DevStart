using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Admin.PromoCodes.DeactivatePromoCode
{
    public sealed record DeactivatePromoCodeCommand(Guid PromoCodeId) : ICommand;
}
