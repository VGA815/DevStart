using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Startups;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.StartupInvestors.GetAllByStartupId
{
    internal sealed class GetStartupInvestorsByStartupIdQueryHandler(IApplicationDbContext context)
        : IQueryHandler<GetStartupInvestorsByStartupIdQuery, List<StartupInvestorResponse>>
    {
        public async Task<Result<List<StartupInvestorResponse>>> Handle(GetStartupInvestorsByStartupIdQuery query, CancellationToken cancellationToken)
        {
            if (!await context.Startups.AnyAsync(s => s.Id == query.StartupId, cancellationToken))
            {
                return Result.Failure<List<StartupInvestorResponse>>(StartupErrors.NotFound(query.StartupId));
            }

            List<StartupInvestorResponse> startupInvestorResponses = await context.StartupInvestors
                .Where(si => si.IsPublic && si.StartupId == query.StartupId)
                .Select(si => new StartupInvestorResponse
                {
                    StartupId = si.StartupId,
                    IsPublic = si.IsPublic,
                    CreatedAt = si.CreatedAt,
                    ProfileId = si.ProfileId,
                    UpdatedAt = si.UpdatedAt,
                })
                .ToListAsync(cancellationToken);

            return startupInvestorResponses;
        }
    }
}
