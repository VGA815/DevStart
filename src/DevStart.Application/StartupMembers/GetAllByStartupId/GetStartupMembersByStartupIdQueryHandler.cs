using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Startups;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.StartupMembers.GetAllByStartupId
{
    internal sealed class GetStartupMembersByStartupIdQueryHandler(IApplicationDbContext context, IUserContext userContext)
        : IQueryHandler<GetStartupMembersByStartupIdQuery, List<StartupMemberResponse>>
    {
        public async Task<Result<List<StartupMemberResponse>>> Handle(GetStartupMembersByStartupIdQuery query, CancellationToken cancellationToken)
        {
            if (!await context.Startups.AnyAsync(sm => sm.Id == query.StartupId, cancellationToken))
            {
                return Result.Failure<List<StartupMemberResponse>>(StartupErrors.NotFound(query.StartupId));
            }

            bool isMember = await context.StartupMembers.AnyAsync(sm => sm.ProfileId == userContext.UserId && sm.StartupId == query.StartupId, cancellationToken);

            List<StartupMemberResponse> startupMemberResponses = await context.StartupMembers
                .Where(sm => sm.StartupId == query.StartupId && (isMember || sm.IsPublic))
                .Select(sm => new StartupMemberResponse
                {
                    StartupId = sm.StartupId,
                    CreatedAt = sm.CreatedAt,
                    IsPublic = sm.IsPublic,
                    ProfileId = sm.ProfileId,
                    Role = sm.Role,
                    UpdatedAt = sm.UpdatedAt,
                })
                .ToListAsync(cancellationToken);

            return startupMemberResponses;


        }
    }
}
