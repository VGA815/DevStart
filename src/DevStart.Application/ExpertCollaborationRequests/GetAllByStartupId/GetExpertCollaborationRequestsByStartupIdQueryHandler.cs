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

            // Every row in this list belongs to the same startup, so its name is one lookup rather than
            // a correlated subquery per row.
            string startupName = await context.Startups
                .AsNoTracking()
                .Where(s => s.Id == query.StartupId)
                .Select(s => s.Name)
                .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

            IQueryable<ExpertCollaborationRequest> requests = context.ExpertCollaborationRequests
                .AsNoTracking()
                .Where(r => r.StartupId == query.StartupId);

            if (query.Status is { } status)
            {
                requests = requests.Where(r => r.Status == status);
            }

            int pageSize = CollaborationRequestPaging.Size(query.PageSize);
            int skip = CollaborationRequestPaging.Skip(query.PageNumber, pageSize);

            // Pending first so the inbox stays actionable across pages, newest first within each group.
            List<ExpertCollaborationRequestResponse> items = await (
                from r in requests
                join p in context.Profiles.AsNoTracking() on r.ExpertProfileId equals p.UserId into profileMatches
                from p in profileMatches.DefaultIfEmpty()
                orderby r.Status == ExpertCollaborationRequestStatus.Pending ? 0 : 1, r.CreatedAt descending
                select new ExpertCollaborationRequestResponse
                {
                    Id = r.Id,
                    ExpertProfileId = r.ExpertProfileId,
                    ExpertDisplayName = p != null ? p.Name : string.Empty,
                    StartupId = r.StartupId,
                    StartupName = startupName,
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
