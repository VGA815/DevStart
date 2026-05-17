using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Experts;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.ExpertProfiles.GetById
{
    internal sealed class GetExpertProfileByIdQueryHandler(IApplicationDbContext context)
        : IQueryHandler<GetExpertProfileByIdQuery, ExpertProfileResponse>
    {
        public async Task<Result<ExpertProfileResponse>> Handle(GetExpertProfileByIdQuery query, CancellationToken cancellationToken)
        {
            ExpertProfile? expertProfile = await context.ExpertProfiles
                .AsNoTracking()
                .SingleOrDefaultAsync(ep => ep.UserId == query.UserId, cancellationToken);

            if (expertProfile is null)
            {
                return Result.Failure<ExpertProfileResponse>(ExpertProfileErrors.NotFound(query.UserId));
            }

            List<ExpertSpecialization> specializations = await context.ExpertProfileSpecializations
                .AsNoTracking()
                .Where(s => s.ExpertProfileId == expertProfile.Id)
                .Select(s => s.Specialization)
                .ToListAsync(cancellationToken);

            int experiencesCount = await context.ExpertExperiences
                .AsNoTracking()
                .CountAsync(e => e.ExpertProfileId == expertProfile.Id, cancellationToken);

            return new ExpertProfileResponse
            {
                Id = expertProfile.Id,
                UserId = expertProfile.UserId,
                DisplayName = expertProfile.DisplayName,
                Bio = expertProfile.Bio,
                Website = expertProfile.Website,
                IsPublic = expertProfile.IsPublic,
                LinkedInUrl = expertProfile.LinkedInUrl,
                TwitterUrl = expertProfile.TwitterUrl,
                GitHubUrl = expertProfile.GitHubUrl,
                TelegramUrl = expertProfile.TelegramUrl,
                Specializations = specializations,
                ExperiencesCount = experiencesCount,
                CreatedAt = expertProfile.CreatedAt,
                UpdatedAt = expertProfile.UpdatedAt
            };
        }
    }
}
