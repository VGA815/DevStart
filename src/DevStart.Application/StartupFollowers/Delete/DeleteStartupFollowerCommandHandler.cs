using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.StartupFollowers;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.StartupFollowers.Delete
{
    internal sealed class DeleteStartupFollowerCommandHandler(IApplicationDbContext context, IUserContext userContext)
        : ICommandHandler<DeleteStartupFollowerCommand>
    {
        public async Task<Result> Handle(DeleteStartupFollowerCommand command, CancellationToken cancellationToken)
        {
            StartupFollower? startupFollower = await context.StartupFollowers
                .SingleOrDefaultAsync(sf => sf.StartupId == command.StartupId && sf.ProfileId == userContext.UserId, cancellationToken);

            if (startupFollower == null)
            {
                return Result.Failure(StartupFollowerErrors.NotFound(command.StartupId, userContext.UserId));
            }

            context.StartupFollowers.Remove(startupFollower);

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
