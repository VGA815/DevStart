using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Auth.OAuth;
using DevStart.Application.UserConsents;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace DevStart.Application.Users.Login
{
    public sealed class LoginUserCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        ITokenProvider tokenProvider,
        IRefreshTokenService refreshTokenService,
        IConsentService consentService,
        IPendingRegistrationStore pendingStore,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<LoginUserCommand, OAuthAuthResult>
    {
        private static readonly TimeSpan PendingTtl = TimeSpan.FromMinutes(15);

        // A precomputed, well-formed hash (64-hex hash + 32-hex salt) used only to run the verifier on
        // the user-not-found path. Running PBKDF2 anyway keeps the response time of an unknown email
        // comparable to a wrong-password attempt, so timing can't be used to enumerate accounts.
        private const string DummyPasswordHash =
            "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF-0123456789ABCDEF0123456789ABCDEF";

        public async Task<Result<OAuthAuthResult>> Handle(LoginUserCommand command, CancellationToken cancellationToken)
        {
            User? user = await context.Users
                .SingleOrDefaultAsync(u => u.Email == command.Email, cancellationToken);

            if (user is null || string.IsNullOrEmpty(user.PasswordHash))
            {
                // Equalize timing with the real verification path below to avoid leaking whether the
                // email is registered.
                passwordHasher.Verify(command.Password, DummyPasswordHash);
                return Result.Failure<OAuthAuthResult>(UserErrors.NotFoundByEmail);
            }

            bool verified = passwordHasher.Verify(command.Password, user.PasswordHash);

            if (!verified)
            {
                return Result.Failure<OAuthAuthResult>(UserErrors.NotFoundByEmail);
            }

            if (!user.IsVerified)
            {
                return Result.Failure<OAuthAuthResult>(UserErrors.EmailNotVerified);
            }

            // Re-consent gate: if mandatory consents are outdated (e.g. an admin activated a new document
            // version), require acceptance before issuing tokens. The client completes the challenge via
            // POST api/auth/oauth/complete using the returned pending token.
            if (!await consentService.AreMandatoryConsentsCurrentAsync(user.Id, cancellationToken))
            {
                string pendingToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
                await pendingStore.SaveAsync(
                    pendingToken,
                    new PendingExternalRegistration(default, string.Empty, user.Email, user.IsVerified, null, user.Id),
                    PendingTtl,
                    cancellationToken);

                IReadOnlyList<RequiredConsent> required = await consentService.GetRequiredConsentsAsync(cancellationToken);
                return OAuthAuthResult.ConsentRequired(new ConsentChallenge(pendingToken, required));
            }

            string accessToken = tokenProvider.CreateAccessToken(user);
            IssuedRefreshToken refresh = await refreshTokenService.IssueAsync(
                user,
                command.IpAddress,
                command.UserAgent,
                cancellationToken);

            return OAuthAuthResult.Authenticated(
                new TokenPair(accessToken, refresh.RawToken, tokenProvider.AccessTokenLifetimeSeconds));
        }
    }
}
