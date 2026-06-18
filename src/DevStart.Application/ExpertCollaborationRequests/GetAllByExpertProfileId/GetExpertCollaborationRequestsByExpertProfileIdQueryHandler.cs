using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.ExpertCollaborationRequests.GetById;
using DevStart.Domain.ExpertCollaborationRequests;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.ExpertCollaborationRequests.GetAllByExpertProfileId
{
    internal sealed class GetExpertCollaborationRequestsByExpertProfileIdQueryHandler(
        IApplicationDbContext context,
        IUserContext userContext)
        : IQueryHandler<GetExpertCollaborationRequestsByExpertProfileIdQuery, List<ExpertCollaborationRequestResponse>>
    {
        public async Task<Result<List<ExpertCollaborationRequestResponse>>> Handle(GetExpertCollaborationRequestsByExpertProfileIdQuery query, CancellationToken cancellationToken)
        {
            if (query.ExpertProfileId != userContext.UserId)
            {
                return Result.Failure<List<ExpertCollaborationRequestResponse>>(
                    ExpertCollaborationRequestErrors.Unauthorized);
            }

            List<ExpertCollaborationRequestResponse> requests = await context.ExpertCollaborationRequests
                .AsNoTracking()
                .Where(r => r.ExpertProfileId == query.ExpertProfileId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ExpertCollaborationRequestResponse
                {
                    Id = r.Id,
                    ExpertProfileId = r.ExpertProfileId,
                    ExpertDisplayName = context.Profiles
                        .Where(p => p.UserId == r.ExpertProfileId)
                        .Select(p => p.Name)
                        .FirstOrDefault() ?? string.Empty,
                    StartupId = r.StartupId,
                    StartupName = context.Startups
                        .Where(s => s.Id == r.StartupId)
                        .Select(s => s.Name)
                        .FirstOrDefault() ?? string.Empty,
                    CollaborationType = r.CollaborationType,
                    Message = r.Message,
                    ProposedHoursPerWeek = r.ProposedHoursPerWeek,
                    ProposedRate = r.ProposedRate,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt
                })
                .ToListAsync(cancellationToken);

            return requests;
        }
    }
}
