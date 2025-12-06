using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Profiles;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Profiles.Update
{
    internal sealed class UpdateProfileCommandHandler(IApplicationDbContext context, IUserContext userContext)
        : ICommandHandler<UpdateProfileCommand>
    {
        public async Task<Result> Handle(UpdateProfileCommand command, CancellationToken cancellationToken)
        {
            Profile? profile = await context.Profiles
                .SingleOrDefaultAsync(p => p.UserId == command.UserId, cancellationToken);

            if (profile == null)
            {
                return Result.Failure(ProfileErrors.NotFound(command.UserId));
            }

            profile.AvatarUrl = command.AvatarUrl;
            profile.IsPublic = command.IsPublic;
            profile.SocialMediaLinks = command.SocialMediaLinks;
            profile.Bio = command.Bio;
            profile.IsAvailableForHire = command.IsAvailableForHire;
            profile.Name = command.Name;
            profile.Url = command.Url;

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
