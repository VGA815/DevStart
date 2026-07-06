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

namespace DevStart.Application.Auth.TwoFactor.VerifyLogin
{
    internal sealed class VerifyTwoFactorLoginCommandHandler(
        IApplicationDbContext context,
        IPendingTwoFactorStore pendingStore,
        IPendingRegistrationStore pendingRegistrationStore,
        ITwoFactorCodeVerifier codeVerifier,
        ITokenProvider tokenProvider,
        IRefreshTokenService refreshTokenService,
        IConsentService consentService,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<VerifyTwoFactorLoginCommand, OAuthAuthResult>
    {
        internal const int MaxAttempts = 5;
        private static readonly TimeSpan ConsentPendingTtl = TimeSpan.FromMinutes(15);

        public async Task<Result<OAuthAuthResult>> Handle(
            VerifyTwoFactorLoginCommand command, CancellationToken cancellationToken)
        {
            PendingTwoFactorLogin? pending = await pendingStore.GetAsync(command.PendingToken, cancellationToken);
            if (pending is null)
            {
                return Result.Failure<OAuthAuthResult>(TwoFactorErrors.ChallengeExpired);
            }
            if (pending.SetupRequired)
            {
                // This token belongs to the mandatory-enrollment flow (api/auth/2fa/setup).
                return Result.Failure<OAuthAuthResult>(TwoFactorErrors.SetupRequired);
            }

            User? user = await context.Users
                .SingleOrDefaultAsync(u => u.Id == pending.UserId, cancellationToken);
            UserTwoFactor? twoFactor = await context.UserTwoFactors
                .SingleOrDefaultAsync(t => t.UserId == pending.UserId, cancellationToken);

            if (user is null || twoFactor is null || !twoFactor.IsEnabled)
            {
                await pendingStore.RemoveAsync(command.PendingToken, cancellationToken);
                return Result.Failure<OAuthAuthResult>(TwoFactorErrors.ChallengeExpired);
            }

            DateTime now = dateTimeProvider.UtcNow;
            if (user.IsCurrentlyBanned(now))
            {
                await pendingStore.RemoveAsync(command.PendingToken, cancellationToken);
                return Result.Failure<OAuthAuthResult>(UserErrors.Banned);
            }

            if (!await codeVerifier.VerifyAndConsumeAsync(twoFactor, command.Code, cancellationToken))
            {
                long attempts = await pendingStore.IncrementAttemptsAsync(command.PendingToken, cancellationToken);
                if (attempts >= MaxAttempts)
                {
                    await pendingStore.RemoveAsync(command.PendingToken, cancellationToken);
                    return Result.Failure<OAuthAuthResult>(TwoFactorErrors.TooManyAttempts);
                }
                return Result.Failure<OAuthAuthResult>(TwoFactorErrors.InvalidCode);
            }

            // Persist the consumed timestep / used recovery code before the challenge is released
            // or any token is issued, so the same code can never be accepted twice. LastUsedTimestep
            // is a concurrency token: of two concurrent submissions of the same code only one save
            // wins, the other lands here.
            try
            {
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure<OAuthAuthResult>(TwoFactorErrors.InvalidCode);
            }
            await pendingStore.RemoveAsync(command.PendingToken, cancellationToken);

            // Same re-consent gate as password login; the pending record marks 2FA as already
            // satisfied so the completion handler does not challenge a second time.
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
                return OAuthAuthResult.ConsentRequired(new ConsentChallenge(consentToken, required));
            }

            string accessToken = tokenProvider.CreateAccessToken(user);
            IssuedRefreshToken refresh = await refreshTokenService.IssueAsync(
                user, command.IpAddress, command.UserAgent, cancellationToken);

            return OAuthAuthResult.Authenticated(
                new TokenPair(accessToken, refresh.RawToken, tokenProvider.AccessTokenLifetimeSeconds));
        }
    }
}
