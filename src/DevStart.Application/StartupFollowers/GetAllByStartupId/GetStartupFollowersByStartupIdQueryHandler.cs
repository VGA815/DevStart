using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Abstractions.Pagination;
using DevStart.Domain.Startups;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.StartupFollowers.GetAllByStartupId
{
    internal sealed class GetStartupFollowersByStartupIdQueryHandler(IApplicationDbContext context)
        : IQueryHandler<GetStartupFollowersByStartupIdQuery, List<StartupFollowerResponse>>
    {
        public async Task<Result<List<StartupFollowerResponse>>> Handle(GetStartupFollowersByStartupIdQuery query, CancellationToken cancellationToken)
        {
            if (!await context.Startups.AnyAsync(s => s.Id == query.StartupId, cancellationToken))
            {
                return Result.Failure<List<StartupFollowerResponse>>(StartupErrors.NotFound(query.StartupId));
            }

            (int page, int pageSize) = Paging.Normalize(query.Page, query.PageSize);

            List<StartupFollowerResponse> startupFollowerResponses = await context.StartupFollowers
                .Where(sf => sf.StartupId == query.StartupId)
                .Select(sf => new StartupFollowerResponse
                {
                    StartupId = sf.StartupId,
                    CreatedAt = sf.CreatedAt,
                    ProfileId = sf.ProfileId,
                })
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return startupFollowerResponses;
        }
    }
}
