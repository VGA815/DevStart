using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.ExternalLogins;
using DevStart.Domain.Profiles;
using DevStart.Domain.UserPreferences;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Auth.OAuth.Callback
{
    internal sealed class HandleOAuthCallbackCommandHandler(
        IApplicationDbContext context,
        IOAuthStateStore stateStore,
        IExternalAuthProviderFactory providerFactory,
        ITokenProvider tokenProvider,
        IRefreshTokenService refreshTokenService,
        IDateTimeProvider dateTimeProvider)
        : ICommandHandler<HandleOAuthCallbackCommand, TokenPair>
    {
        public async Task<Result<TokenPair>> Handle(
            HandleOAuthCallbackCommand command,
            CancellationToken cancellationToken)
        {
            OAuthStateEntry? state = await stateStore.ConsumeAsync(command.State, cancellationToken);
            if (state is null || state.Provider != command.Provider)
            {
                return Result.Failure<TokenPair>(ExternalLoginErrors.InvalidState);
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
            catch
            {
                return Result.Failure<TokenPair>(ExternalLoginErrors.ProviderError);
            }

            DateTime now = dateTimeProvider.UtcNow;

            ExternalLogin? existing = await context.ExternalLogins
                .FirstOrDefaultAsync(
                    x => x.Provider == command.Provider && x.ProviderUserId == info.ProviderUserId,
                    cancellationToken);

            User user;

            if (state.LinkUserId.HasValue)
            {
                Result<User> linkResult = await LinkBranchAsync(
                    state.LinkUserId.Value,
                    command.Provider,
                    info,
                    existing,
                    now,
                    cancellationToken);
                if (linkResult.IsFailure)
                {
                    return Result.Failure<TokenPair>(linkResult.Error);
                }
                user = linkResult.Value;
            }
            else
            {
                Result<User> loginResult = await LoginBranchAsync(
                    command.Provider,
                    info,
                    existing,
                    now,
                    cancellationToken);
                if (loginResult.IsFailure)
                {
                    return Result.Failure<TokenPair>(loginResult.Error);
                }
                user = loginResult.Value;
            }

            await context.SaveChangesAsync(cancellationToken);

            string accessToken = tokenProvider.CreateAccessToken(user);
            IssuedRefreshToken refresh = await refreshTokenService.IssueAsync(
                user,
                command.IpAddress,
                command.UserAgent,
                cancellationToken);

            return new TokenPair(accessToken, refresh.RawToken, tokenProvider.AccessTokenLifetimeSeconds);
        }

        private async Task<Result<User>> LoginBranchAsync(
            ExternalLoginProvider providerKind,
            ExternalUserInfo info,
            ExternalLogin? existing,
            DateTime now,
            CancellationToken cancellationToken)
        {
            if (existing is not null)
            {
                User? linkedUser = await context.Users
                    .FirstOrDefaultAsync(u => u.Id == existing.UserId, cancellationToken);
                if (linkedUser is null)
                {
                    return Result.Failure<User>(ExternalLoginErrors.NotFound);
                }
                existing.Touch(now);
                return linkedUser;
            }

            if (string.IsNullOrWhiteSpace(info.Email))
            {
                return Result.Failure<User>(ExternalLoginErrors.EmailRequired);
            }

            User? userByEmail = await context.Users
                .FirstOrDefaultAsync(u => u.Email == info.Email, cancellationToken);

            if (userByEmail is not null)
            {
                if (!userByEmail.IsVerified || !info.EmailVerified)
                {
                    return Result.Failure<User>(ExternalLoginErrors.EmailMatchesUnverifiedAccount);
                }

                ExternalLogin link = ExternalLogin.Create(
                    userByEmail.Id, providerKind, info.ProviderUserId, info.Email, now);
                link.Raise(new UserLinkedExternalLoginDomainEvent(
                    userByEmail.Id, providerKind, info.ProviderUserId));
                context.ExternalLogins.Add(link);

                return userByEmail;
            }

            string username = await GenerateUniqueUsernameAsync(info, providerKind, cancellationToken);
            User newUser = User.CreateExternal(username, info.Email!, info.EmailVerified, now);
            newUser.Raise(new UserRegisteredDomainEvent(newUser.Id, newUser.Email));
            context.Users.Add(newUser);

            Profile profile = Profile.Create(
                newUser.Id, info.Name, null, null, false, false, null);
            context.Profiles.Add(profile);

            UserPreference preference = UserPreference.Create(
                newUser.Id, UserPreferenceTheme.System);
            context.Preferences.Add(preference);

            ExternalLogin newLink = ExternalLogin.Create(
                newUser.Id, providerKind, info.ProviderUserId, info.Email, now);
            newLink.Raise(new UserLinkedExternalLoginDomainEvent(
                newUser.Id, providerKind, info.ProviderUserId));
            context.ExternalLogins.Add(newLink);

            return newUser;
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

        private async Task<string> GenerateUniqueUsernameAsync(
            ExternalUserInfo info,
            ExternalLoginProvider providerKind,
            CancellationToken cancellationToken)
        {
            string baseName = !string.IsNullOrWhiteSpace(info.Name)
                ? SlugifyUsername(info.Name!)
                : $"{providerKind.ToString().ToLowerInvariant()}_{info.ProviderUserId}";

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
