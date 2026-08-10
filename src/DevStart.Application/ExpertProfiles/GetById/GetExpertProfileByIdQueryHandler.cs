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
            var data = await context.ExpertProfiles
                .AsNoTracking()
                .Where(ep => ep.UserId == query.UserId)
                .Select(ep => new
                {
                    ep.Id,
                    ep.UserId,
                    ep.CreatedAt,
                    ep.UpdatedAt,
                    ep.Profile.Name,
                    ep.Profile.Bio,
                    ep.Profile.Url,
                    ep.Profile.IsPublic,
                    ep.Profile.AvatarId,
                    ep.Profile.LinkedInUrl,
                    ep.Profile.TwitterUrl,
                    ep.Profile.GitHubUrl,
                    ep.Profile.TelegramUrl
                })
                .SingleOrDefaultAsync(cancellationToken);

            if (data is null)
            {
                return Result.Failure<ExpertProfileResponse>(ExpertProfileErrors.NotFound(query.UserId));
            }

            List<ExpertSpecialization> specializations = await context.ExpertProfileSpecializations
                .AsNoTracking()
                .Where(s => s.ExpertProfileId == data.Id)
                .Select(s => s.Specialization)
                .ToListAsync(cancellationToken);

            int experiencesCount = await context.ExpertExperiences
                .AsNoTracking()
                .CountAsync(e => e.ExpertProfileId == data.Id, cancellationToken);

            return new ExpertProfileResponse
            {
                Id = data.Id,
                UserId = data.UserId,
                DisplayName = data.Name ?? string.Empty,
                Bio = data.Bio,
                Website = data.Url,
                IsPublic = data.IsPublic,
                AvatarId = data.AvatarId,
                LinkedInUrl = data.LinkedInUrl,
                TwitterUrl = data.TwitterUrl,
                GitHubUrl = data.GitHubUrl,
                TelegramUrl = data.TelegramUrl,
                Specializations = specializations,
                ExperiencesCount = experiencesCount,
                CreatedAt = data.CreatedAt,
                UpdatedAt = data.UpdatedAt
            };
        }
    }
}
