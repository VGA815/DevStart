using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.ExternalLogins;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Auth.OAuth.Unlink
{
    internal sealed class UnlinkExternalLoginCommandHandler(
        IApplicationDbContext context)
        : ICommandHandler<UnlinkExternalLoginCommand>
    {
        public async Task<Result> Handle(UnlinkExternalLoginCommand command, CancellationToken cancellationToken)
        {
            User? user = await context.Users
                .FirstOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);
            if (user is null)
            {
                return Result.Failure(UserErrors.NotFound(command.UserId));
            }

            ExternalLogin? link = await context.ExternalLogins
                .FirstOrDefaultAsync(
                    x => x.UserId == command.UserId && x.Provider == command.Provider,
                    cancellationToken);
            if (link is null)
            {
                return Result.Failure(ExternalLoginErrors.NotFound);
            }

            int totalLinks = await context.ExternalLogins
                .CountAsync(x => x.UserId == command.UserId, cancellationToken);

            bool hasPassword = !string.IsNullOrEmpty(user.PasswordHash);
            int remainingCredentials = (totalLinks - 1) + (hasPassword ? 1 : 0);

            if (remainingCredentials <= 0)
            {
                return Result.Failure(ExternalLoginErrors.CannotUnlinkLastCredential);
            }

            context.ExternalLogins.Remove(link);
            await context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
