using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Startups.GetAll
{
    internal sealed class GetStartupsQueryHandler(IApplicationDbContext context)
        : IQueryHandler<GetStartupsQuery, List<StartupResponse>>
    {
        public async Task<Result<List<StartupResponse>>> Handle(GetStartupsQuery query, CancellationToken cancellationToken)
        {
            var startupQuery = context.Startups.AsQueryable();

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
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt
                })
                .ToListAsync(cancellationToken);

            return startups;
        }
    }
}
