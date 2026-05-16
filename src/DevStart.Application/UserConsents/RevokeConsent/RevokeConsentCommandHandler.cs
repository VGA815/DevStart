using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.UserConsents;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.UserConsents.RevokeConsent
{
    internal sealed class RevokeConsentCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IDateTimeProvider dateTimeProvider)
        : ICommandHandler<RevokeConsentCommand>
    {
        public async Task<Result> Handle(RevokeConsentCommand command, CancellationToken cancellationToken)
        {
            if (ConsentVersions.MandatoryTypes.Contains(command.ConsentType))
            {
                return Result.Failure(UserConsentErrors.CannotRevokeMandatoryConsent);
            }

            Guid userId = userContext.UserId;

            UserConsent? consent = await context.UserConsents
                .FirstOrDefaultAsync(
                    uc => uc.UserId == userId &&
                          uc.Type == command.ConsentType &&
                          uc.RevokedAt == null,
                    cancellationToken);

            if (consent is null)
            {
                return Result.Failure(UserConsentErrors.ConsentNotFound(command.ConsentType));
            }

            consent.Revoke(dateTimeProvider.UtcNow);

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
