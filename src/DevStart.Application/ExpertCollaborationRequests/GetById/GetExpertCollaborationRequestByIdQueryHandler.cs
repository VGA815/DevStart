using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.ExpertCollaborationRequests;
using DevStart.Domain.StartupMembers;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.ExpertCollaborationRequests.GetById
{
    internal sealed class GetExpertCollaborationRequestByIdQueryHandler(
        IApplicationDbContext context,
        IUserContext userContext)
        : IQueryHandler<GetExpertCollaborationRequestByIdQuery, ExpertCollaborationRequestResponse>
    {
        public async Task<Result<ExpertCollaborationRequestResponse>> Handle(GetExpertCollaborationRequestByIdQuery query, CancellationToken cancellationToken)
        {
            ExpertCollaborationRequest? request = await context.ExpertCollaborationRequests
                .AsNoTracking()
                .SingleOrDefaultAsync(r => r.Id == query.RequestId, cancellationToken);

            if (request is null)
            {
                return Result.Failure<ExpertCollaborationRequestResponse>(
                    ExpertCollaborationRequestErrors.NotFound(query.RequestId));
            }

            Guid userId = userContext.UserId;
            bool isExpert = request.ExpertProfileId == userId;
            bool isFounderOrAdmin = false;

            if (!isExpert)
            {
                isFounderOrAdmin = await context.StartupMembers
                    .AsNoTracking()
                    .AnyAsync(sm => sm.StartupId == request.StartupId
                                 && sm.ProfileId == userId
                                 && (sm.Role == StartupRole.Founder || sm.Role == StartupRole.Administration),
                              cancellationToken);
            }

            if (!isExpert && !isFounderOrAdmin)
            {
                return Result.Failure<ExpertCollaborationRequestResponse>(
                    ExpertCollaborationRequestErrors.Unauthorized);
            }

            string expertDisplayName = await context.ExpertProfiles
                .AsNoTracking()
                .Where(ep => ep.Id == request.ExpertProfileId)
                .Select(ep => ep.DisplayName)
                .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

            string startupName = await context.Startups
                .AsNoTracking()
                .Where(s => s.Id == request.StartupId)
                .Select(s => s.Name)
                .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

            return new ExpertCollaborationRequestResponse
            {
                Id = request.Id,
                ExpertProfileId = request.ExpertProfileId,
                ExpertDisplayName = expertDisplayName,
                StartupId = request.StartupId,
                StartupName = startupName,
                CollaborationType = request.CollaborationType,
                Message = request.Message,
                ProposedHoursPerWeek = request.ProposedHoursPerWeek,
                ProposedRate = request.ProposedRate,
                Status = request.Status,
                CreatedAt = request.CreatedAt,
                UpdatedAt = request.UpdatedAt
            };
        }
    }
}
