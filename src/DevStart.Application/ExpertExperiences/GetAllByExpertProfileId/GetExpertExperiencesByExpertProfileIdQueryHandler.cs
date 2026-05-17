using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.ExpertExperiences.GetAllByExpertProfileId
{
    internal sealed class GetExpertExperiencesByExpertProfileIdQueryHandler(IApplicationDbContext context)
        : IQueryHandler<GetExpertExperiencesByExpertProfileIdQuery, List<ExpertExperienceResponse>>
    {
        public async Task<Result<List<ExpertExperienceResponse>>> Handle(
            GetExpertExperiencesByExpertProfileIdQuery query,
            CancellationToken cancellationToken)
        {
            List<ExpertExperienceResponse> result = await context.ExpertExperiences
                .AsNoTracking()
                .Where(e => e.ExpertProfileId == query.ExpertProfileId)
                .OrderByDescending(e => e.StartDate)
                .Select(e => new ExpertExperienceResponse
                {
                    Id              = e.Id,
                    ExpertProfileId = e.ExpertProfileId,
                    Company         = e.Company,
                    Position        = e.Position,
                    StartDate       = e.StartDate,
                    EndDate         = e.EndDate,
                    Description     = e.Description,
                    CreatedAt       = e.CreatedAt,
                    UpdatedAt       = e.UpdatedAt
                })
                .ToListAsync(cancellationToken);

            return result;
        }
    }
}
