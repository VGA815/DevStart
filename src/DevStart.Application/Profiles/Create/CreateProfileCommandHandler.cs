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


            Profile profile = new Profile()
            {
                Name = command.Name,
                AvatarUrl = command.AvatarUrl,
                Bio = command.Bio,
                IsAvailableForHire = command.IsAvailableForHire,
                IsPublic = command.IsPublic,
                SocialMediaLinks = command.SocialMediaLinks,
                Url = command.Url,
                UserId = command.UserId,
            };

            context.Profiles.Add(profile);

            await context.SaveChangesAsync(cancellationToken);

            return profile.UserId;
        }
    }
}
