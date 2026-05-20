using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.ExpertCollaborationRequests.GetById;
using DevStart.Domain.ExpertCollaborationRequests;
using DevStart.Domain.StartupMembers;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.ExpertCollaborationRequests.GetAllByStartupId
{
    internal sealed class GetExpertCollaborationRequestsByStartupIdQueryHandler(
        IApplicationDbContext context,
        IUserContext userContext)
        : IQueryHandler<GetExpertCollaborationRequestsByStartupIdQuery, List<ExpertCollaborationRequestResponse>>
    {
        public async Task<Result<List<ExpertCollaborationRequestResponse>>> Handle(GetExpertCollaborationRequestsByStartupIdQuery query, CancellationToken cancellationToken)
        {
            bool isFounderOrAdmin = await context.StartupMembers
                .AsNoTracking()
                .AnyAsync(sm => sm.StartupId == query.StartupId
                             && sm.ProfileId == userContext.UserId
                             && (sm.Role == StartupRole.Founder || sm.Role == StartupRole.Administration),
                          cancellationToken);

            if (!isFounderOrAdmin)
            {
                return Result.Failure<List<ExpertCollaborationRequestResponse>>(
                    ExpertCollaborationRequestErrors.Unauthorized);
            }

            List<ExpertCollaborationRequestResponse> requests = await context.ExpertCollaborationRequests
                .AsNoTracking()
                .Where(r => r.StartupId == query.StartupId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ExpertCollaborationRequestResponse
                {
                    Id = r.Id,
                    ExpertProfileId = r.ExpertProfileId,
                    ExpertDisplayName = context.ExpertProfiles
                        .Where(ep => ep.Id == r.ExpertProfileId)
                        .Select(ep => ep.DisplayName)
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
