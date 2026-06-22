using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Startups.GetAll
{
    internal sealed class GetStartupsQueryHandler(IApplicationDbContext context, IDateTimeProvider dateTimeProvider)
        : IQueryHandler<GetStartupsQuery, List<StartupResponse>>
    {
        public async Task<Result<List<StartupResponse>>> Handle(GetStartupsQuery query, CancellationToken cancellationToken)
        {
            // Banned startups are hidden from public discovery. We honour "lazy expiry": a temporary ban
            // whose BanExpiresAt has passed is treated as lifted immediately, without waiting for the
            // hourly ban-expiry job to clear the flag.
            DateTime now = dateTimeProvider.UtcNow;
            var startupQuery = context.Startups
                .Where(s => !(s.IsBanned && (s.BanExpiresAt == null || s.BanExpiresAt > now)));

            if (query.Stage.HasValue)
            {
                startupQuery = startupQuery.Where(s => s.Stage == query.Stage);
            }

            if (query.Location.HasValue)
            {
                startupQuery = startupQuery.Where(s => s.Location == query.Location);
            }

            if (query.IsStopped.HasValue)
            {
                startupQuery = startupQuery.Where(s => s.IsStopped == query.IsStopped);
            }

            List<StartupResponse> startups = await startupQuery
                .OrderByDescending(s => context.StartupFollowers.Count(f => f.StartupId == s.Id))
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(s => new StartupResponse
                {
                    Id = s.Id,
                    ShortDescription = s.ShortDescription,
                    Name = s.Name,
                    PublicEmail = s.PublicEmail,
                    Description = s.Description,
                    Url = s.Url,
                    IsStopped = s.IsStopped,
                    Stage = s.Stage,
                    SocialMediaLinks = s.SocialMediaLinks,
                    Location = s.Location,
                    BillingEmail = s.BillingEmail,
                    AvatarId = s.AvatarId,
                    Tam = s.Tam,
                    Sam = s.Sam,
                    Som = s.Som,
                    MarketGrowthRate = s.MarketGrowthRate,
                    HasPatents = s.HasPatents,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt
                })
                .ToListAsync(cancellationToken);

            return startups;
        }
    }
}
