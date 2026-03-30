using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Profiles;
using DevStart.Domain.UserPreferences;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Users.Register
{
    internal sealed class RegisterUserCommandHandler(IApplicationDbContext context, IPasswordHasher passwordHasher, IDateTimeProvider dateTimeProvider)
        : ICommandHandler<RegisterUserCommand, Guid>
    {
        public async Task<Result<Guid>> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
        {
            if (await context.Users.AnyAsync(u => u.Email == command.Email, cancellationToken))
            {
                return Result.Failure<Guid>(UserErrors.EmailNotUnique);
            }

            User user = User.Create(command.Username, command.Email, passwordHasher.Hash(command.Password), dateTimeProvider.UtcNow);
            Profile profile = Profile.Create(user.Id, command.Name, command.Bio, command.Url, false, command.IsPublic, null);
            UserPreference userPreference = UserPreference.Create(user.Id, UserPreferenceTheme.System);

            user.Raise(new UserRegisteredDomainEvent(user.Id, user.Email));

            context.Users.Add(user);
            context.Preferences.Add(userPreference);
            context.Profiles.Add(profile);

            await context.SaveChangesAsync(cancellationToken);

            return user.Id;
        }
    }
}
