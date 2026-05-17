using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.InvestorProfiles.GetAll;

internal sealed class GetInvestorProfilesQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetInvestorProfilesQuery, List<InvestorCatalogResponse>>
{
    public async Task<Result<List<InvestorCatalogResponse>>> Handle(
        GetInvestorProfilesQuery query,
        CancellationToken cancellationToken)
    {
        var q = context.InvestorProfiles
            .AsNoTracking()
            .Where(ip => ip.IsPublic);

        if (query.Type.HasValue)
            q = q.Where(ip => ip.Type == query.Type);

        q = query.SortBy switch
        {
            InvestorSortBy.CreatedAt => q.OrderByDescending(ip => ip.CreatedAt),
            _                        => q.OrderBy(ip => ip.DisplayName)
        };

        List<InvestorCatalogResponse> result = await q
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(ip => new InvestorCatalogResponse
            {
                Id          = ip.Id,
                UserId      = ip.UserId,
                Type        = ip.Type,
                DisplayName = ip.DisplayName,
                Bio         = ip.Bio,
                Website     = ip.Website,
                CreatedAt   = ip.CreatedAt,
                UpdatedAt   = ip.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return result;
    }
}
