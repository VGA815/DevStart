using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Startups;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.StartupCompetitors.GetAllByStartupId
{
    internal sealed class GetStartupCompetitorsByStartupIdQueryHandler(IApplicationDbContext context)
        : IQueryHandler<GetStartupCompetitorsByStartupIdQuery, List<StartupCompetitorResponse>>
    {
        public async Task<Result<List<StartupCompetitorResponse>>> Handle(GetStartupCompetitorsByStartupIdQuery query, CancellationToken cancellationToken)
        {
            if (!await context.Startups.AnyAsync(s => s.Id == query.StartupId, cancellationToken))
            {
                return Result.Failure<List<StartupCompetitorResponse>>(StartupErrors.NotFound(query.StartupId));
            }

            List<StartupCompetitorResponse> competitors = await context.StartupCompetitors
                .AsNoTracking()
                .Where(c => c.StartupId == query.StartupId)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new StartupCompetitorResponse
                {
                    Id = c.Id,
                    StartupId = c.StartupId,
                    Name = c.Name,
                    Website = c.Website,
                    Description = c.Description,
                    StrengthsVsUs = c.StrengthsVsUs,
                    WeaknessesVsUs = c.WeaknessesVsUs,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                })
                .ToListAsync(cancellationToken);

            return competitors;
        }
    }
}
