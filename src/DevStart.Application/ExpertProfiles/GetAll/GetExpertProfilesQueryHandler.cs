using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Abstractions.Pagination;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.ExpertProfiles.GetAll;

internal sealed class GetExpertProfilesQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetExpertProfilesQuery, List<ExpertCatalogResponse>>
{
    public async Task<Result<List<ExpertCatalogResponse>>> Handle(
        GetExpertProfilesQuery query,
        CancellationToken cancellationToken)
    {
        var q = context.ExpertProfiles
            .AsNoTracking()
            .Where(ep => ep.Profile.IsPublic);

        if (query.Specialization.HasValue)
        {
            var specialization = query.Specialization.Value;
            q = q.Where(ep => context.ExpertProfileSpecializations
                .Any(s => s.ExpertProfileId == ep.Id && s.Specialization == specialization));
        }

        q = query.SortBy switch
        {
            ExpertSortBy.CreatedAt => q.OrderByDescending(ep => ep.CreatedAt),
            _                      => q.OrderBy(ep => ep.Profile.Name)
        };

        (int pageNumber, int pageSize) = Paging.Normalize(query.PageNumber, query.PageSize);

        List<ExpertCatalogResponse> result = await q
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(ep => new ExpertCatalogResponse
            {
                Id              = ep.Id,
                UserId          = ep.UserId,
                DisplayName     = ep.Profile.Name ?? string.Empty,
                Bio             = ep.Profile.Bio,
                Website         = ep.Profile.Url,
                LinkedInUrl     = ep.Profile.LinkedInUrl,
                TwitterUrl      = ep.Profile.TwitterUrl,
                GitHubUrl       = ep.Profile.GitHubUrl,
                TelegramUrl     = ep.Profile.TelegramUrl,
                Specializations = context.ExpertProfileSpecializations
                                    .Where(s => s.ExpertProfileId == ep.Id)
                                    .Select(s => s.Specialization)
                                    .ToList(),
                ExperiencesCount = context.ExpertExperiences
                                    .Count(e => e.ExpertProfileId == ep.Id),
                CreatedAt       = ep.CreatedAt,
                UpdatedAt       = ep.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return result;
    }
}
