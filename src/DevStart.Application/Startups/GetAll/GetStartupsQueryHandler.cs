using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.StartupCommunityStandards;
using DevStart.Domain.Startups;
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
                .AsNoTracking()
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

            // Single left join to the checklist projection: it both feeds the badge and backs the level
            // filter, so the database sees one join against a primary key instead of a correlated
            // subquery per row. Joined before paging because the filter has to narrow the set the page
            // is taken from.
            var joined = from s in startupQuery
                         join standards in context.StartupCommunityStandards
                             on s.Id equals standards.StartupId into standardsGroup
                         from cs in standardsGroup.DefaultIfEmpty()
                         select new { Startup = s, Standards = cs };

            if (query.MinCommunityStandardsLevel is { } minLevel && minLevel > CommunityStandardsLevel.Incomplete)
            {
                // A startup with no projection row has not been evaluated yet, so it cannot claim a level
                // above Incomplete and drops out here. Filtering by Incomplete itself is a no-op: the
                // listing already reports an unevaluated startup as Incomplete, so excluding it would
                // contradict the badge it is shown with.
                joined = joined.Where(x => x.Standards != null && x.Standards.Level >= minLevel);
            }

            // Projected to scalars rather than entities: nothing is materialized into the change tracker,
            // and only the columns the response needs cross the wire.
            var rows = await joined
                .OrderByDescending(x => context.StartupFollowers.Count(f => f.StartupId == x.Startup.Id))
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(x => new
                {
                    x.Startup.Id,
                    x.Startup.Name,
                    x.Startup.PublicEmail,
                    x.Startup.Description,
                    x.Startup.ShortDescription,
                    x.Startup.Url,
                    x.Startup.IsStopped,
                    x.Startup.Stage,
                    x.Startup.SocialMediaLinks,
                    x.Startup.Location,
                    x.Startup.BillingEmail,
                    x.Startup.AvatarId,
                    x.Startup.Tam,
                    x.Startup.Sam,
                    x.Startup.Som,
                    x.Startup.MarketGrowthRate,
                    x.Startup.HasPatents,
                    x.Startup.CreatedAt,
                    x.Startup.UpdatedAt,
                    CompletedCount = (int?)x.Standards.CompletedCount,
                    TotalCount = (int?)x.Standards.TotalCount,
                    Level = (CommunityStandardsLevel?)x.Standards.Level
                })
                .ToListAsync(cancellationToken);

            // The percentage is worked out here so the division doesn't have to survive a translation
            // to SQL. A startup with no projection row reads as 0% / Incomplete, matching what the
            // level filter above assumes.
            return rows.Select(row =>
            {
                int completed = row.CompletedCount ?? 0;
                int total = row.TotalCount ?? 0;

                return new StartupResponse
                {
                    Id = row.Id,
                    ShortDescription = row.ShortDescription,
                    Name = row.Name,
                    PublicEmail = row.PublicEmail,
                    Description = row.Description,
                    Url = row.Url,
                    IsStopped = row.IsStopped,
                    Stage = row.Stage,
                    SocialMediaLinks = row.SocialMediaLinks,
                    Location = row.Location,
                    BillingEmail = row.BillingEmail,
                    AvatarId = row.AvatarId,
                    Tam = row.Tam,
                    Sam = row.Sam,
                    Som = row.Som,
                    MarketGrowthRate = row.MarketGrowthRate,
                    HasPatents = row.HasPatents,
                    CommunityStandardsPercent = total > 0
                        ? Math.Round(completed * 100m / total, 0, MidpointRounding.AwayFromZero)
                        : 0m,
                    CommunityStandardsLevel = row.Level ?? CommunityStandardsLevel.Incomplete,
                    CreatedAt = row.CreatedAt,
                    UpdatedAt = row.UpdatedAt
                };
            }).ToList();
        }
    }
}
