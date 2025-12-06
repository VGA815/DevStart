using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.UserPreferences;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.UserPreferences.Create
{
    internal sealed class CreateUserPreferenceCommandHandler(IApplicationDbContext context, IUserContext userContext)
        : ICommandHandler<CreateUserPreferenceCommand, Guid>
    {
        public async Task<Result<Guid>> Handle(CreateUserPreferenceCommand command, CancellationToken cancellationToken)
        {
            if (command.UserId != userContext.UserId)
            {
                return Result.Failure<Guid>(UserErrors.Unauthorized());
            }

            User? user = await context.Users.AsNoTracking()
                .SingleOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);

            if (user == null)
            {
                return Result.Failure<Guid>(UserErrors.NotFound(command.UserId));
            }

            UserPreference userPreference = new UserPreference()
            {
                UserId = userContext.UserId,
                ReceiveNotifications = command.ReceiveNotifications,
                Theme = command.Theme,
            };

            context.Preferences.Add(userPreference);

            await context.SaveChangesAsync(cancellationToken);

            return userPreference.UserId;
        }
    }
}
