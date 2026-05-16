using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.ConsentDocuments;
using DevStart.Domain.Profiles;
using DevStart.Domain.UserConsents;
using DevStart.Domain.UserPreferences;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Users.Register
{
    internal sealed class RegisterUserCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IDateTimeProvider dateTimeProvider)
        : ICommandHandler<RegisterUserCommand, Guid>
    {
        public async Task<Result<Guid>> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
        {
            if (await context.Users.AnyAsync(u => u.Email == command.Email, cancellationToken))
            {
                return Result.Failure<Guid>(UserErrors.EmailNotUnique);
            }

            // Validate consent document versions against the currently active documents
            Dictionary<ConsentType, string> activeVersions = await context.ConsentDocuments
                .Where(d => d.IsActive)
                .ToDictionaryAsync(d => d.Type, d => d.Version, cancellationToken);

            foreach (ConsentItem consent in command.Consents)
            {
                if (!activeVersions.TryGetValue(consent.Type, out string? activeVersion))
                {
                    return Result.Failure<Guid>(ConsentDocumentErrors.NoActiveDocument(consent.Type));
                }

                if (consent.DocumentVersion != activeVersion)
                {
                    return Result.Failure<Guid>(
                        UserConsentErrors.ConsentVersionMismatch(consent.Type, activeVersion));
                }
            }

            DateTime now = dateTimeProvider.UtcNow;

            User user = User.Create(command.Username, command.Email, passwordHasher.Hash(command.Password), now);
            Profile profile = Profile.Create(user.Id, command.Name, command.Bio, command.Url, false, command.IsPublic, null);
            UserPreference userPreference = UserPreference.Create(user.Id, UserPreferenceTheme.System);

            List<UserConsent> consents = command.Consents
                .Select(c => UserConsent.Create(user.Id, c.Type, c.DocumentVersion, now))
                .ToList();

            user.Raise(new UserRegisteredDomainEvent(user.Id, user.Email));

            context.Users.Add(user);
            context.Preferences.Add(userPreference);
            context.Profiles.Add(profile);
            context.UserConsents.AddRange(consents);

            await context.SaveChangesAsync(cancellationToken);

            return user.Id;
        }
    }
}
