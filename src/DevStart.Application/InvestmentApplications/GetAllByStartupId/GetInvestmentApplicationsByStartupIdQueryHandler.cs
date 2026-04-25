using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.InvestmentApplications;
using DevStart.Domain.StartupMembers;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.InvestmentApplications.GetAllByStartupId
{
    internal sealed class GetInvestmentApplicationsByStartupIdQueryHandler(
        IApplicationDbContext context,
        IUserContext userContext)
        : IQueryHandler<GetInvestmentApplicationsByStartupIdQuery, List<InvestmentApplicationResponse>>
    {
        public async Task<Result<List<InvestmentApplicationResponse>>> Handle(GetInvestmentApplicationsByStartupIdQuery query, CancellationToken cancellationToken)
        {
            bool isFounderOrAdmin = await context.StartupMembers
                .AsNoTracking()
                .AnyAsync(sm => sm.StartupId == query.StartupId
                             && sm.ProfileId == userContext.UserId
                             && (sm.Role == StartupRole.Founder || sm.Role == StartupRole.Administration),
                          cancellationToken);

            if (!isFounderOrAdmin)
            {
                return Result.Failure<List<InvestmentApplicationResponse>>(InvestmentApplicationErrors.Unauthorized);
            }

            List<InvestmentApplicationResponse> applications = await context.InvestmentApplications
                .AsNoTracking()
                .Where(a => a.StartupId == query.StartupId)
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new InvestmentApplicationResponse
                {
                    Id = a.Id,
                    InvestorProfileId = a.InvestorProfileId,
                    InvestorDisplayName = context.InvestorProfiles
                        .Where(ip => ip.Id == a.InvestorProfileId)
                        .Select(ip => ip.DisplayName)
                        .FirstOrDefault() ?? string.Empty,
                    StartupId = a.StartupId,
                    StartupName = context.Startups
                        .Where(s => s.Id == a.StartupId)
                        .Select(s => s.Name)
                        .FirstOrDefault() ?? string.Empty,
                    RoadmapItemId = a.RoadmapItemId,
                    RoadmapItemTitle = a.RoadmapItemId != null
                        ? context.StartupRoadmapItems
                            .Where(r => r.Id == a.RoadmapItemId)
                            .Select(r => r.Title)
                            .FirstOrDefault()
                        : null,
                    Amount = a.Amount,
                    Message = a.Message,
                    Status = a.Status,
                    CreatedAt = a.CreatedAt,
                    UpdatedAt = a.UpdatedAt
                })
                .ToListAsync(cancellationToken);

            return applications;
        }
    }
}
