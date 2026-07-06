using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Auth.TwoFactor;
using DevStart.SharedKernel;

namespace DevStart.Application.Users.TwoFactor.Enable
{
    internal sealed class EnableTwoFactorCommandHandler(
        IUserContext userContext,
        ITwoFactorEnrollmentService enrollment,
        IRefreshTokenService refreshTokenService) : ICommandHandler<EnableTwoFactorCommand, IReadOnlyList<string>>
    {
        public async Task<Result<IReadOnlyList<string>>> Handle(
            EnableTwoFactorCommand command, CancellationToken cancellationToken)
        {
            Result<IReadOnlyList<string>> confirmed = await enrollment.ConfirmAsync(
                userContext.UserId, command.Code, cancellationToken);
            if (confirmed.IsFailure)
            {
                return confirmed;
            }

            // Credential change: force re-authentication everywhere (project convention, see
            // ChangePasswordCommandHandler). The current access token stays valid until expiry.
            await refreshTokenService.RevokeAllForUserAsync(userContext.UserId, cancellationToken);

            return confirmed;
        }
    }
}
