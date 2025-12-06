using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Profiles;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Profiles.Delete
{
    internal sealed class DeleteProfileCommandHandler(IApplicationDbContext context, IUserContext userContext)
        : ICommandHandler<DeleteProfileCommand>
    {
        public async Task<Result> Handle(DeleteProfileCommand command, CancellationToken cancellationToken)
        {
            Profile? profile = await context.Profiles
                .SingleOrDefaultAsync(p => p.UserId == command.UserId && p.UserId == userContext.UserId);

            if (profile == null)
            {
                return Result.Failure(ProfileErrors.NotFound(command.UserId));
            }

            context.Profiles.Remove(profile);

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
