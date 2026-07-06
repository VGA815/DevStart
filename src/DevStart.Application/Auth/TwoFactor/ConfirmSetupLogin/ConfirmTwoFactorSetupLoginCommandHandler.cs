using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Auth.OAuth;
using DevStart.Application.UserConsents;
using DevStart.Domain.TwoFactor;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace DevStart.Application.Auth.TwoFactor.ConfirmSetupLogin
{
    /// <summary>
    /// Completes mandatory 2FA enrollment during login: confirms the first TOTP code, activates
    /// 2FA, returns the one-time recovery codes and finishes the login (tokens or consent challenge).
    /// </summary>
    internal sealed class ConfirmTwoFactorSetupLoginCommandHandler(
        IApplicationDbContext context,
        IPendingTwoFactorStore pendingStore,
        IPendingRegistrationStore pendingRegistrationStore,
        ITwoFactorEnrollmentService enrollment,
        ITokenProvider tokenProvider,
        IRefreshTokenService refreshTokenService,
        IConsentService consentService,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<ConfirmTwoFactorSetupLoginCommand, TwoFactorSetupCompleteResponse>
    {
        internal const int MaxAttempts = 5;
        private static readonly TimeSpan ConsentPendingTtl = TimeSpan.FromMinutes(15);

        public async Task<Result<TwoFactorSetupCompleteResponse>> Handle(
            ConfirmTwoFactorSetupLoginCommand command, CancellationToken cancellationToken)
        {
            PendingTwoFactorLogin? pending = await pendingStore.GetAsync(command.PendingToken, cancellationToken);
            if (pending is null || !pending.SetupRequired)
            {
                return Result.Failure<TwoFactorSetupCompleteResponse>(TwoFactorErrors.ChallengeExpired);
            }

            User? user = await context.Users
                .SingleOrDefaultAsync(u => u.Id == pending.UserId, cancellationToken);
            if (user is null)
            {
                await pendingStore.RemoveAsync(command.PendingToken, cancellationToken);
                return Result.Failure<TwoFactorSetupCompleteResponse>(TwoFactorErrors.ChallengeExpired);
            }

            DateTime now = dateTimeProvider.UtcNow;
            if (user.IsCurrentlyBanned(now))
            {
                await pendingStore.RemoveAsync(command.PendingToken, cancellationToken);
                return Result.Failure<TwoFactorSetupCompleteResponse>(UserErrors.Banned);
            }

            Result<IReadOnlyList<string>> confirmed = await enrollment.ConfirmAsync(
                user.Id, command.Code, cancellationToken);
            if (confirmed.IsFailure)
            {
                if (confirmed.Error == TwoFactorErrors.InvalidCode)
                {
                    long attempts = await pendingStore.IncrementAttemptsAsync(command.PendingToken, cancellationToken);
                    if (attempts >= MaxAttempts)
                    {
                        await pendingStore.RemoveAsync(command.PendingToken, cancellationToken);
                        return Result.Failure<TwoFactorSetupCompleteResponse>(TwoFactorErrors.TooManyAttempts);
                    }
                }
                return Result.Failure<TwoFactorSetupCompleteResponse>(confirmed.Error);
            }

            await pendingStore.RemoveAsync(command.PendingToken, cancellationToken);

            if (!await consentService.AreMandatoryConsentsCurrentAsync(user.Id, cancellationToken))
            {
                string consentToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
                await pendingRegistrationStore.SaveAsync(
                    consentToken,
                    new PendingExternalRegistration(
                        default, string.Empty, user.Email, user.IsVerified, null, user.Id, TwoFactorSatisfied: true),
                    ConsentPendingTtl,
                    cancellationToken);

                IReadOnlyList<RequiredConsent> required = await consentService.GetRequiredConsentsAsync(cancellationToken);
                return new TwoFactorSetupCompleteResponse(
                    confirmed.Value,
                    OAuthAuthResult.ConsentRequired(new ConsentChallenge(consentToken, required)));
            }

            string accessToken = tokenProvider.CreateAccessToken(user);
            IssuedRefreshToken refresh = await refreshTokenService.IssueAsync(
                user, command.IpAddress, command.UserAgent, cancellationToken);

            return new TwoFactorSetupCompleteResponse(
                confirmed.Value,
                OAuthAuthResult.Authenticated(
                    new TokenPair(accessToken, refresh.RawToken, tokenProvider.AccessTokenLifetimeSeconds)));
        }
    }
}
