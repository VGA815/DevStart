using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Profiles;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Profiles.Create
{
    internal sealed class CreateProfileCommandHandler(IApplicationDbContext context, IUserContext userContext)
        : ICommandHandler<CreateProfileCommand, Guid>
    {
        public async Task<Result<Guid>> Handle(CreateProfileCommand command, CancellationToken cancellationToken)
        {
            if (await context.Profiles.AnyAsync(p => p.UserId == command.UserId, cancellationToken))
            {
                throw new NotImplementedException();
            }
            if (command.UserId != userContext.UserId)
            {
                return Result.Failure<Guid>(UserErrors.Unauthorized());
            }

            User? user = await context.Users.AsNoTracking()
                .SingleOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);

            if (user is null)
            {
                return Result.Failure<Guid>(UserErrors.NotFound(command.UserId));
            }


            Profile profile = new Profile(command.UserId, command.SocialMediaLinks, command.IsAvailableForHire, command.IsPublic, command.Name, command.Url, command.AvatarUrl, command.Bio);

            context.Profiles.Add(profile);

            await context.SaveChangesAsync(cancellationToken);

            return profile.UserId;
        }
    }
}
