using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.UserConsents;
using DevStart.Domain.ExternalLogins;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace DevStart.Application.Auth.OAuth.Callback
{
    internal sealed class HandleOAuthCallbackCommandHandler(
        IApplicationDbContext context,
        IOAuthStateStore stateStore,
        IPendingRegistrationStore pendingStore,
        IExternalAuthProviderFactory providerFactory,
        ITokenProvider tokenProvider,
        IRefreshTokenService refreshTokenService,
        IConsentService consentService,
        IDateTimeProvider dateTimeProvider,
        ILogger<HandleOAuthCallbackCommandHandler> logger)
        : ICommandHandler<HandleOAuthCallbackCommand, OAuthAuthResult>
    {
        private static readonly TimeSpan PendingTtl = TimeSpan.FromMinutes(15);

        public async Task<Result<OAuthAuthResult>> Handle(
            HandleOAuthCallbackCommand command,
            CancellationToken cancellationToken)
        {
            OAuthStateEntry? state = await stateStore.ConsumeAsync(command.State, cancellationToken);
            if (state is null || state.Provider != command.Provider)
            {
                return Result.Failure<OAuthAuthResult>(ExternalLoginErrors.InvalidState);
            }

            IExternalAuthProvider provider = providerFactory.Get(command.Provider);

            ExternalUserInfo info;
            try
            {
                info = await provider.ExchangeCodeAsync(
                    command.Code,
                    state.CodeVerifier,
                    state.RedirectUri,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                logger.LogWarning(ex, "OAuth code exchange failed for provider {Provider}", command.Provider);
                return Result.Failure<OAuthAuthResult>(ExternalLoginErrors.ProviderError);
            }

            DateTime now = dateTimeProvider.UtcNow;

            ExternalLogin? existing = await context.ExternalLogins
                .FirstOrDefaultAsync(
                    x => x.Provider == command.Provider && x.ProviderUserId == info.ProviderUserId,
                    cancellationToken);

            // Linking flow: the user is already authenticated (and has consented at registration).
            if (state.LinkUserId.HasValue)
            {
                Result<User> linkResult = await LinkBranchAsync(
                    state.LinkUserId.Value, command.Provider, info, existing, now, cancellationToken);
                if (linkResult.IsFailure)
                {
                    return Result.Failure<OAuthAuthResult>(linkResult.Error);
                }
                if (linkResult.Value.IsCurrentlyBanned(now))
                {
                    return Result.Failure<OAuthAuthResult>(UserErrors.Banned);
                }

                await context.SaveChangesAsync(cancellationToken);
                return OAuthAuthResult.Authenticated(await IssueTokensAsync(linkResult.Value, command, cancellationToken));
            }

            // Login flow with an already-linked external account.
            if (existing is not null)
            {
                User? linkedUser = await context.Users
                    .FirstOrDefaultAsync(u => u.Id == existing.UserId, cancellationToken);
                if (linkedUser is null)
                {
                    return Result.Failure<OAuthAuthResult>(ExternalLoginErrors.NotFound);
                }

                existing.Touch(now);
                await context.SaveChangesAsync(cancellationToken);

                return await IssueOrChallengeAsync(linkedUser, info, command, cancellationToken);
            }

            if (string.IsNullOrWhiteSpace(info.Email))
            {
                return Result.Failure<OAuthAuthResult>(ExternalLoginErrors.EmailRequired);
            }

            // Login flow where the email matches a local account: link, then issue or challenge.
            User? userByEmail = await context.Users
                .FirstOrDefaultAsync(u => u.Email == info.Email, cancellationToken);

            if (userByEmail is not null)
            {
                if (!userByEmail.IsVerified || !info.EmailVerified)
                {
                    return Result.Failure<OAuthAuthResult>(ExternalLoginErrors.EmailMatchesUnverifiedAccount);
                }

                ExternalLogin link = ExternalLogin.Create(
                    userByEmail.Id, command.Provider, info.ProviderUserId, info.Email, now);
                link.Raise(new UserLinkedExternalLoginDomainEvent(
                    userByEmail.Id, command.Provider, info.ProviderUserId));
                context.ExternalLogins.Add(link);
                await context.SaveChangesAsync(cancellationToken);

                return await IssueOrChallengeAsync(userByEmail, info, command, cancellationToken);
            }

            // Brand-new user: do NOT create the account until consent is accepted.
            string newToken = NewPendingToken();
            await pendingStore.SaveAsync(
                newToken,
                new PendingExternalRegistration(
                    command.Provider, info.ProviderUserId, info.Email!, info.EmailVerified, info.Name, null),
                PendingTtl,
                cancellationToken);

            IReadOnlyList<RequiredConsent> required = await consentService.GetRequiredConsentsAsync(cancellationToken);
            return OAuthAuthResult.ConsentRequired(new ConsentChallenge(newToken, required));
        }

        private async Task<Result<OAuthAuthResult>> IssueOrChallengeAsync(
            User user, ExternalUserInfo info, HandleOAuthCallbackCommand command, CancellationToken cancellationToken)
        {
            if (user.IsCurrentlyBanned(dateTimeProvider.UtcNow))
            {
                return Result.Failure<OAuthAuthResult>(UserErrors.Banned);
            }

            if (await consentService.AreMandatoryConsentsCurrentAsync(user.Id, cancellationToken))
            {
                return OAuthAuthResult.Authenticated(await IssueTokensAsync(user, command, cancellationToken));
            }

            string token = NewPendingToken();
            await pendingStore.SaveAsync(
                token,
                new PendingExternalRegistration(
                    command.Provider, info.ProviderUserId, info.Email ?? user.Email, info.EmailVerified, info.Name, user.Id),
                PendingTtl,
                cancellationToken);

            IReadOnlyList<RequiredConsent> required = await consentService.GetRequiredConsentsAsync(cancellationToken);
            return OAuthAuthResult.ConsentRequired(new ConsentChallenge(token, required));
        }

        private async Task<TokenPair> IssueTokensAsync(
            User user, HandleOAuthCallbackCommand command, CancellationToken cancellationToken)
        {
            string accessToken = tokenProvider.CreateAccessToken(user);
            IssuedRefreshToken refresh = await refreshTokenService.IssueAsync(
                user, command.IpAddress, command.UserAgent, cancellationToken);

            return new TokenPair(accessToken, refresh.RawToken, tokenProvider.AccessTokenLifetimeSeconds);
        }

        private async Task<Result<User>> LinkBranchAsync(
            Guid linkUserId,
            ExternalLoginProvider providerKind,
            ExternalUserInfo info,
            ExternalLogin? existing,
            DateTime now,
            CancellationToken cancellationToken)
        {
            User? user = await context.Users.FirstOrDefaultAsync(u => u.Id == linkUserId, cancellationToken);
            if (user is null)
            {
                return Result.Failure<User>(UserErrors.NotFound(linkUserId));
            }

            if (existing is not null)
            {
                if (existing.UserId != linkUserId)
                {
                    return Result.Failure<User>(ExternalLoginErrors.AlreadyLinkedToAnotherUser);
                }
                existing.Touch(now);
                return user;
            }

            ExternalLogin link = ExternalLogin.Create(
                linkUserId, providerKind, info.ProviderUserId, info.Email, now);
            link.Raise(new UserLinkedExternalLoginDomainEvent(linkUserId, providerKind, info.ProviderUserId));
            context.ExternalLogins.Add(link);

            return user;
        }

        private static string NewPendingToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    }
}
