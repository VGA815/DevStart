using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
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
            .Where(ep => ep.IsPublic);

        if (query.Specialization.HasValue)
        {
            var specialization = query.Specialization.Value;
            q = q.Where(ep => context.ExpertProfileSpecializations
                .Any(s => s.ExpertProfileId == ep.Id && s.Specialization == specialization));
        }

        q = query.SortBy switch
        {
            ExpertSortBy.CreatedAt => q.OrderByDescending(ep => ep.CreatedAt),
            _                      => q.OrderBy(ep => ep.DisplayName)
        };

        List<ExpertCatalogResponse> result = await q
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(ep => new ExpertCatalogResponse
            {
                Id              = ep.Id,
                UserId          = ep.UserId,
                DisplayName     = ep.DisplayName,
                Bio             = ep.Bio,
                Website         = ep.Website,
                LinkedInUrl     = ep.LinkedInUrl,
                TwitterUrl      = ep.TwitterUrl,
                GitHubUrl       = ep.GitHubUrl,
                TelegramUrl     = ep.TelegramUrl,
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
