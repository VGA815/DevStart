using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Startups.GetAllByProfileId
{
    internal sealed class GetStartupsByProfileIdQueryHandler(IApplicationDbContext context, IDateTimeProvider dateTimeProvider)
        : IQueryHandler<GetStartupsByProfileIdQuery, List<StartupResponse>>
    {
        public async Task<Result<List<StartupResponse>>> Handle(GetStartupsByProfileIdQuery query, CancellationToken cancellationToken)
        {
            DateTime now = dateTimeProvider.UtcNow;
            List<Guid> startupMemberIds = await context.StartupMembers
                .Where(sm => sm.ProfileId == query.ProfileId)
                .Select(sm => sm.StartupId)
                .ToListAsync(cancellationToken);
            List<Guid> startupInvestorIds = await context.StartupInvestors
                .Where(si => si.ProfileId == query.ProfileId && si.IsPublic)
                .Select(si => si.StartupId)
                .ToListAsync(cancellationToken);

            List<StartupResponse> startups = await context.Startups
                .Where(s => (startupMemberIds.Contains(s.Id) || startupInvestorIds.Contains(s.Id))
                         && !(s.IsBanned && (s.BanExpiresAt == null || s.BanExpiresAt > now)))
                .Select(s => new StartupResponse
                {
                    Id = s.Id,
                    AvatarId = s.AvatarId,
                    ShortDescription = s.ShortDescription,
                    CreatedAt = DateTime.UtcNow,
                    Description = s.Description,
                    IsStopped = s.IsStopped,
                    Location = s.Location,
                    Name = s.Name,
                    PublicEmail = s.PublicEmail,
                    SocialMediaLinks = s.SocialMediaLinks,
                    Stage = s.Stage,
                    UpdatedAt = DateTime.UtcNow,
                    Url = s.Url,
                    Tam = s.Tam,
                    Sam = s.Sam,
                    Som = s.Som,
                    MarketGrowthRate = s.MarketGrowthRate,
                    HasPatents = s.HasPatents,
                })
                .ToListAsync(cancellationToken);

            return startups;
        }
    }
}
