using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Startups.GetAllByProfileId;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.StartupFollowers.GetAllByProfileId
{
    internal sealed class GetStartupsByProfileFollowsQueryHandler(IApplicationDbContext context, IDateTimeProvider dateTimeProvider)
        : IQueryHandler<GetStartupsByProfileFollowsQuery, List<StartupResponse>>
    {
        public async Task<Result<List<StartupResponse>>> Handle(
            GetStartupsByProfileFollowsQuery query,
            CancellationToken cancellationToken)
        {
            DateTime now = dateTimeProvider.UtcNow;

            List<Guid> followedIds = await context.StartupFollowers
                .Where(sf => sf.ProfileId == query.ProfileId)
                .Select(sf => sf.StartupId)
                .ToListAsync(cancellationToken);

            if (followedIds.Count == 0)
                return new List<StartupResponse>();

            List<StartupResponse> startups = await context.Startups
                .Where(s => followedIds.Contains(s.Id)
                         && !(s.IsBanned && (s.BanExpiresAt == null || s.BanExpiresAt > now)))
                .Select(s => new StartupResponse
                {
                    Id = s.Id,
                    AvatarId = s.AvatarId,
                    BillingEmail = s.BillingEmail,
                    ShortDescription = s.ShortDescription,
                    CreatedAt = s.CreatedAt,
                    Description = s.Description,
                    IsStopped = s.IsStopped,
                    Location = s.Location,
                    Name = s.Name,
                    PublicEmail = s.PublicEmail,
                    SocialMediaLinks = s.SocialMediaLinks,
                    Stage = s.Stage,
                    UpdatedAt = s.UpdatedAt,
                    Url = s.Url,
                })
                .ToListAsync(cancellationToken);

            return startups;
        }
    }
}
