using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
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
        IDateTimeProvider dateTimeProvider)
        : ICommandHandler<CompleteOAuthRegistrationCommand, TokenPair>
    {
        public async Task<Result<TokenPair>> Handle(
            CompleteOAuthRegistrationCommand command,
            CancellationToken cancellationToken)
        {
            PendingExternalRegistration? pending = await pendingStore.ConsumeAsync(command.PendingToken, cancellationToken);
            if (pending is null)
            {
                return Result.Failure<TokenPair>(ExternalLoginErrors.InvalidState);
            }

            DateTime now = dateTimeProvider.UtcNow;
            User user;

            if (pending.ExistingUserId is Guid existingUserId)
            {
                User? existingUser = await context.Users
                    .FirstOrDefaultAsync(u => u.Id == existingUserId, cancellationToken);
                if (existingUser is null)
                {
                    return Result.Failure<TokenPair>(ExternalLoginErrors.NotFound);
                }

                Result<List<UserConsent>> consentsResult = await consentService.BuildAcceptedConsentsAsync(
                    existingUser.Id, command.Consents, now, cancellationToken);
                if (consentsResult.IsFailure)
                {
                    return Result.Failure<TokenPair>(consentsResult.Error);
                }

                context.UserConsents.AddRange(consentsResult.Value);
                user = existingUser;
            }
            else
            {
                if (await context.Users.AnyAsync(u => u.Email == pending.Email, cancellationToken))
                {
                    return Result.Failure<TokenPair>(UserErrors.EmailNotUnique);
                }

                string username = await GenerateUniqueUsernameAsync(pending, cancellationToken);
                User newUser = User.CreateExternal(username, pending.Email, pending.EmailVerified, now);

                Result<List<UserConsent>> consentsResult = await consentService.BuildAcceptedConsentsAsync(
                    newUser.Id, command.Consents, now, cancellationToken);
                if (consentsResult.IsFailure)
                {
                    return Result.Failure<TokenPair>(consentsResult.Error);
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

            await context.SaveChangesAsync(cancellationToken);

            string accessToken = tokenProvider.CreateAccessToken(user);
            IssuedRefreshToken refresh = await refreshTokenService.IssueAsync(
                user, command.IpAddress, command.UserAgent, cancellationToken);

            return new TokenPair(accessToken, refresh.RawToken, tokenProvider.AccessTokenLifetimeSeconds);
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
