using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Profiles;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Profiles.GetById
{
    internal sealed class GetProfileByIdQueryHandler(IApplicationDbContext context, IUserContext userContext)
        : IQueryHandler<GetProfileByIdQuery, ProfileResponse>
    {
        public async Task<Result<ProfileResponse>> Handle(GetProfileByIdQuery query, CancellationToken cancellationToken)
        {
            if (query.UserId != userContext.UserId)
            {
                return Result.Failure<ProfileResponse>(UserErrors.Unauthorized());
            }

            ProfileResponse? profile = await context.Profiles
                .Where(p => p.UserId == query.UserId)
                .Select(p => new ProfileResponse
                {
                    UserId = p.UserId,
                    AvatarUrl = p.AvatarUrl,
                    Bio = p.Bio,
                    IsAvailableForHire = p.IsAvailableForHire,
                    IsPublic = p.IsPublic,
                    Name = p.Name,
                    SocialMediaLinks = p.SocialMediaLinks,
                    Url = p.Url,
                })
                .SingleOrDefaultAsync(cancellationToken);

            if (profile is null)
            {
                return Result.Failure<ProfileResponse>(ProfileErrors.NotFound(query.UserId));
            }

            return profile;
        }
    }
}
