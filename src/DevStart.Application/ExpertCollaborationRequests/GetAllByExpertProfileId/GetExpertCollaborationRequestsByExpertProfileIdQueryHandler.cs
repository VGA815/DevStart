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

            // Every row belongs to the same expert, so the display name is one lookup rather than a
            // correlated subquery per row.
            string expertDisplayName = await context.Profiles
                .AsNoTracking()
                .Where(p => p.UserId == query.ExpertProfileId)
                .Select(p => p.Name)
                .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

            IQueryable<ExpertCollaborationRequest> requests = context.ExpertCollaborationRequests
                .AsNoTracking()
                .Where(r => r.ExpertProfileId == query.ExpertProfileId);

            if (query.Status is { } status)
            {
                requests = requests.Where(r => r.Status == status);
            }

            int pageSize = CollaborationRequestPaging.Size(query.PageSize);
            int skip = CollaborationRequestPaging.Skip(query.PageNumber, pageSize);

            // Pending first so invitations awaiting an answer stay actionable across pages.
            List<ExpertCollaborationRequestResponse> items = await (
                from r in requests
                join s in context.Startups.AsNoTracking() on r.StartupId equals s.Id into startupMatches
                from s in startupMatches.DefaultIfEmpty()
                orderby r.Status == ExpertCollaborationRequestStatus.Pending ? 0 : 1, r.CreatedAt descending
                select new ExpertCollaborationRequestResponse
                {
                    Id = r.Id,
                    ExpertProfileId = r.ExpertProfileId,
                    ExpertDisplayName = expertDisplayName,
                    StartupId = r.StartupId,
                    StartupName = s != null ? s.Name : string.Empty,
                    Initiator = r.Initiator,
                    CollaborationType = r.CollaborationType,
                    Message = r.Message,
                    ProposedHoursPerWeek = r.ProposedHoursPerWeek,
                    ProposedRate = r.ProposedRate,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt
                })
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return items;
        }
    }
}
