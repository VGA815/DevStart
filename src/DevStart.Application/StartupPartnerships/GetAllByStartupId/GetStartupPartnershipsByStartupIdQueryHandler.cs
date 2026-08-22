using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Startups;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.StartupPartnerships.GetAllByStartupId
{
    internal sealed class GetStartupPartnershipsByStartupIdQueryHandler(IApplicationDbContext context)
        : IQueryHandler<GetStartupPartnershipsByStartupIdQuery, List<StartupPartnershipResponse>>
    {
        public async Task<Result<List<StartupPartnershipResponse>>> Handle(
            GetStartupPartnershipsByStartupIdQuery query, CancellationToken cancellationToken)
        {
            if (!await context.Startups.AnyAsync(s => s.Id == query.StartupId, cancellationToken))
            {
                return Result.Failure<List<StartupPartnershipResponse>>(StartupErrors.NotFound(query.StartupId));
            }

            List<StartupPartnershipResponse> partnerships = await context.StartupPartnerships
                .AsNoTracking()
                .Where(p => p.StartupId == query.StartupId)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new StartupPartnershipResponse
                {
                    Id = p.Id,
                    StartupId = p.StartupId,
                    PartnerName = p.PartnerName,
                    Website = p.Website,
                    Kind = p.Kind,
                    Description = p.Description,

                    // Spelled out rather than reading the entity's IsWorkedOut property: this has to
                    // translate to SQL, and an unmapped property would throw at query time.
                    IsWorkedOut = p.Description != null && p.Description.Trim() != "",
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt
                })
                .ToListAsync(cancellationToken);

            return partnerships;
        }
    }
}
