using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Startups;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.StartupMembers.GetAllByStartupId
{
    internal sealed class GetStartupMembersByStartupIdQueryHandler(IApplicationDbContext context)
        : IQueryHandler<GetStartupMembersByStartupIdQuery, List<StartupMemberResponse>>
    {
        public async Task<Result<List<StartupMemberResponse>>> Handle(GetStartupMembersByStartupIdQuery query, CancellationToken cancellationToken)
        {
            if (!await context.Startups.AnyAsync(sm => sm.Id == query.StartupId, cancellationToken))
            {
                return Result.Failure<List<StartupMemberResponse>>(StartupErrors.NotFound(query.StartupId));
            }

            List<StartupMemberResponse> startupMemberResponses = await context.StartupMembers
                .Where(sm => sm.StartupId == query.StartupId && sm.IsPublic)
                .Select(sm => new StartupMemberResponse
                {
                    StartupId = sm.StartupId,
                    CreatedAt = sm.CreatedAt,
                    IsPublic = sm.IsPublic,
                    ProfileId = sm.ProfileId,
                    Role = sm.Role,
                    Position = sm.Position,
                    Name = sm.Profile.Name,
                    Bio = sm.Profile.Bio,
                    YearsOfExperience = sm.YearsOfExperience,
                    HasPriorExit = sm.HasPriorExit,
                    PreviousStartupsCount = sm.PreviousStartupsCount,
                    UpdatedAt = sm.UpdatedAt,
                })
                .ToListAsync(cancellationToken);

            return startupMemberResponses;
        }
    }
}
