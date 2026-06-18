using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Startups;
using DevStart.Domain.ExpertCollaborationRequests;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.ExpertCollaborationRequests.GetById
{
    internal sealed class GetExpertCollaborationRequestByIdQueryHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IStartupAuthorizationService authorization)
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

            if (!isExpert && !await authorization.IsFounderOrAdminAsync(userId, request.StartupId, cancellationToken))
            {
                return Result.Failure<ExpertCollaborationRequestResponse>(
                    ExpertCollaborationRequestErrors.Unauthorized);
            }

            string expertDisplayName = await context.Profiles
                .AsNoTracking()
                .Where(p => p.UserId == request.ExpertProfileId)
                .Select(p => p.Name)
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
