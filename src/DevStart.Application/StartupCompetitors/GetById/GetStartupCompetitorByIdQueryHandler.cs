using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.StartupCompetitors;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.StartupCompetitors.GetById
{
    internal sealed class GetStartupCompetitorByIdQueryHandler(IApplicationDbContext context)
        : IQueryHandler<GetStartupCompetitorByIdQuery, StartupCompetitorResponse>
    {
        public async Task<Result<StartupCompetitorResponse>> Handle(GetStartupCompetitorByIdQuery query, CancellationToken cancellationToken)
        {
            StartupCompetitorResponse? competitor = await context.StartupCompetitors
                .AsNoTracking()
                .Where(c => c.Id == query.CompetitorId)
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
                .SingleOrDefaultAsync(cancellationToken);

            if (competitor is null)
            {
                return Result.Failure<StartupCompetitorResponse>(StartupCompetitorErrors.NotFound(query.CompetitorId));
            }

            return competitor;
        }
    }
}
