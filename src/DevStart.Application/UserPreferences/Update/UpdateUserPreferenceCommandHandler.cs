using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.UserPreferences;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.UserPreferences.Update
{
    internal sealed class UpdateUserPreferenceCommandHandler(IApplicationDbContext context, IUserContext userContext)
        : ICommandHandler<UpdateUserPreferenceCommand>
    {
        public async Task<Result> Handle(UpdateUserPreferenceCommand command, CancellationToken cancellationToken)
        {
            UserPreference? userPreference = await context.Preferences
                .SingleOrDefaultAsync(up => up.UserId == command.UserId && up.UserId == userContext.UserId, cancellationToken);

            if (userPreference == null)
            {
                return Result.Failure(UserPreferenceErrors.NotFound(command.UserId));
            }

            userPreference.ReceiveNotifications = command.ReceiveNotifications;
            userPreference.Theme = command.Theme;

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
