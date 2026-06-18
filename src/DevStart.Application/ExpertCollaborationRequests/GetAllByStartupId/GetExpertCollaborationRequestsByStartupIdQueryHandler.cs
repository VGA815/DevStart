using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.ExpertCollaborationRequests.GetById;
using DevStart.Application.Startups;
using DevStart.Domain.ExpertCollaborationRequests;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.ExpertCollaborationRequests.GetAllByStartupId
{
    internal sealed class GetExpertCollaborationRequestsByStartupIdQueryHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IStartupAuthorizationService authorization)
        : IQueryHandler<GetExpertCollaborationRequestsByStartupIdQuery, List<ExpertCollaborationRequestResponse>>
    {
        public async Task<Result<List<ExpertCollaborationRequestResponse>>> Handle(GetExpertCollaborationRequestsByStartupIdQuery query, CancellationToken cancellationToken)
        {
            if (!await authorization.IsFounderOrAdminAsync(userContext.UserId, query.StartupId, cancellationToken))
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
