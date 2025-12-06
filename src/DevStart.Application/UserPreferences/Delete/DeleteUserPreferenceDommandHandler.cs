using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.UserPreferences;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.UserPreferences.Delete
{
    internal sealed class DeleteUserPreferenceDommandHandler(IApplicationDbContext context, IUserContext userContext)
        : ICommandHandler<DeleteUserPreferenceCommand>
    {
        public async Task<Result> Handle(DeleteUserPreferenceCommand command, CancellationToken cancellationToken)
        {
            UserPreference? userPreference = await context.Preferences
                .SingleOrDefaultAsync(up => up.UserId == command.UserId && up.UserId == userContext.UserId, cancellationToken);

            if (userPreference is null)
            {
                return Result.Failure(UserPreferenceErrors.NotFound(command.UserId));
            }

            context.Preferences.Remove(userPreference);

            await  context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
