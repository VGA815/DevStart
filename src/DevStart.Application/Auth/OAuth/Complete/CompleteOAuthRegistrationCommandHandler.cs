using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Auth.TwoFactor;
using DevStart.Application.UserConsents;
using DevStart.Domain.ExternalLogins;
using DevStart.Domain.Profiles;
using DevStart.Domain.UserConsents;
using DevStart.Domain.UserPreferences;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Auth.OAuth.Complete
{
    internal sealed class CompleteOAuthRegistrationCommandHandler(
        IApplicationDbContext context,
        IPendingRegistrationStore pendingStore,
        IConsentService consentService,
        ITokenProvider tokenProvider,
        IRefreshTokenService refreshTokenService,
        ITwoFactorLoginGate twoFactorGate,
        IDateTimeProvider dateTimeProvider)
        : ICommandHandler<CompleteOAuthRegistrationCommand, OAuthAuthResult>
    {
        public async Task<Result<OAuthAuthResult>> Handle(
            CompleteOAuthRegistrationCommand command,
            CancellationToken cancellationToken)
        {
            PendingExternalRegistration? pending = await pendingStore.ConsumeAsync(command.PendingToken, cancellationToken);
            if (pending is null)
            {
                return Result.Failure<OAuthAuthResult>(ExternalLoginErrors.InvalidState);
            }

            DateTime now = dateTimeProvider.UtcNow;
            User user;

            if (pending.ExistingUserId is Guid existingUserId)
            {
                User? existingUser = await context.Users
                    .FirstOrDefaultAsync(u => u.Id == existingUserId, cancellationToken);
                if (existingUser is null)
                {
                    return Result.Failure<OAuthAuthResult>(ExternalLoginErrors.NotFound);
                }

                Result<List<UserConsent>> consentsResult = await consentService.BuildAcceptedConsentsAsync(
                    existingUser.Id, command.Consents, now, cancellationToken);
                if (consentsResult.IsFailure)
                {
                    return Result.Failure<OAuthAuthResult>(consentsResult.Error);
                }

                context.UserConsents.AddRange(consentsResult.Value);
                user = existingUser;
            }
            else
            {
                if (await context.Users.AnyAsync(u => u.Email == pending.Email, cancellationToken))
                {
                    return Result.Failure<OAuthAuthResult>(UserErrors.EmailNotUnique);
                }

                string username = await GenerateUniqueUsernameAsync(pending, cancellationToken);
                User newUser = User.CreateExternal(username, pending.Email, pending.EmailVerified, now);

                Result<List<UserConsent>> consentsResult = await consentService.BuildAcceptedConsentsAsync(
                    newUser.Id, command.Consents, now, cancellationToken);
                if (consentsResult.IsFailure)
                {
                    return Result.Failure<OAuthAuthResult>(consentsResult.Error);
                }

                newUser.Raise(new UserRegisteredDomainEvent(newUser.Id, newUser.Email));
                context.Users.Add(newUser);

                Profile profile = Profile.Create(newUser.Id, pending.Name, null, null, false, false, null);
                context.Profiles.Add(profile);

                UserPreference preference = UserPreference.Create(newUser.Id, UserPreferenceTheme.System);
                context.Preferences.Add(preference);

                ExternalLogin link = ExternalLogin.Create(
                    newUser.Id, pending.Provider, pending.ProviderUserId, pending.Email, now);
                link.Raise(new UserLinkedExternalLoginDomainEvent(
                    newUser.Id, pending.Provider, pending.ProviderUserId));
                context.ExternalLogins.Add(link);

                context.UserConsents.AddRange(consentsResult.Value);
                user = newUser;
            }

            // A re-consenting existing user could have been banned in the meantime.
            if (user.IsCurrentlyBanned(now))
            {
                return Result.Failure<OAuthAuthResult>(UserErrors.Banned);
            }

            // Persist the accepted consents before any further challenge, so a 2FA round-trip
            // does not ask the user to re-accept them.
            await context.SaveChangesAsync(cancellationToken);

            // Existing users are re-challenged unless this pending record was created after the 2FA
            // gate had already been passed (login → 2FA → consent). Covers the edge case of 2FA
            // being enabled between the consent challenge and its completion. Brand-new users
            // cannot have 2FA yet.
            if (pending.ExistingUserId is not null && !pending.TwoFactorSatisfied)
            {
                // No device token here: reaching this branch means 2FA was turned on mid-flow, so
                // whatever this browser was trusted for predates it.
                OAuthAuthResult? twoFactorChallenge = await twoFactorGate.ChallengeIfRequiredAsync(
                    user, command.IpAddress, command.UserAgent, deviceToken: null, cancellationToken);
                if (twoFactorChallenge is not null)
                {
                    return twoFactorChallenge;
                }
            }

            string accessToken = tokenProvider.CreateAccessToken(user);
            IssuedRefreshToken refresh = await refreshTokenService.IssueAsync(
                user, command.IpAddress, command.UserAgent, cancellationToken);

            return OAuthAuthResult.Authenticated(
                new TokenPair(accessToken, refresh.RawToken, tokenProvider.AccessTokenLifetimeSeconds));
        }

        private async Task<string> GenerateUniqueUsernameAsync(
            PendingExternalRegistration pending,
            CancellationToken cancellationToken)
        {
            string baseName = !string.IsNullOrWhiteSpace(pending.Name)
                ? SlugifyUsername(pending.Name!)
                : $"{pending.Provider.ToString().ToLowerInvariant()}_{pending.ProviderUserId}";

            if (baseName.Length > 90)
            {
                baseName = baseName.Substring(0, 90);
            }

            string candidate = baseName;
            int suffix = 1;
            while (await context.Users.AnyAsync(u => u.Username == candidate, cancellationToken))
            {
                candidate = $"{baseName}_{suffix++}";
                if (suffix > 1000)
                {
                    candidate = $"{baseName}_{Guid.NewGuid():N}".Substring(0, 100);
                    break;
                }
            }
            return candidate;
        }

        private static string SlugifyUsername(string raw)
        {
            var sb = new System.Text.StringBuilder(raw.Length);
            foreach (char ch in raw)
            {
                if (char.IsLetterOrDigit(ch)) sb.Append(char.ToLowerInvariant(ch));
                else if (ch is ' ' or '-' or '_' or '.') sb.Append('_');
            }
            string slug = sb.ToString().Trim('_');
            return string.IsNullOrEmpty(slug) ? "user" : slug;
        }
    }
}
