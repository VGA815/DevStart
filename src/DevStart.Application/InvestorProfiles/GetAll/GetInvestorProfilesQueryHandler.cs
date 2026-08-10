using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Abstractions.Pagination;
using DevStart.Domain.Investors;
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
            .Where(ip => ip.Profile.IsPublic);

        if (query.Type.HasValue)
            q = q.Where(ip => ip.Type == query.Type);

        q = query.SortBy switch
        {
            InvestorSortBy.CreatedAt => q.OrderByDescending(ip => ip.CreatedAt),
            _                        => q.OrderBy(ip => ip.Profile.Name)
        };

        (int pageNumber, int pageSize) = Paging.Normalize(query.PageNumber, query.PageSize);

        List<InvestorCatalogResponse> result = await q
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(ip => new InvestorCatalogResponse
            {
                Id          = ip.Id,
                UserId      = ip.UserId,
                Type        = ip.Type,
                DisplayName = ip.Profile.Name ?? string.Empty,
                Bio         = ip.Profile.Bio,
                Website     = ip.Profile.Url,
                // Фонд показывается своим логотипом (без подстановки фото владельца — если логотипа
                // нет, клиент рисует инициалы названия), физлицо — аватаркой основного аккаунта.
                AvatarId    = ip.Type == InvestorProfileType.Fund ? ip.AvatarId : ip.Profile.AvatarId,
                CreatedAt   = ip.CreatedAt,
                UpdatedAt   = ip.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return result;
    }
}
