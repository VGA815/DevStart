using Microsoft.EntityFrameworkCore;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.SharedKernel;

namespace DevStart.Application.InviteTokens.GetAllByStartupId
{
    internal sealed class GetInviteTokensByStartupIdQueryHandler(IApplicationDbContext context)
        : IQueryHandler<GetInviteTokensByStartupIdQuery, List<InviteTokenResponse>>
    {
        public async Task<Result<List<InviteTokenResponse>>> Handle(GetInviteTokensByStartupIdQuery query, CancellationToken cancellationToken)
        {
            List<InviteTokenResponse> inviteTokens = await context.InviteTokens
                .Where(it => it.StartupId == query.StartupId)
                .Select(it => new InviteTokenResponse
                {
                    Id = it.Id,
                    StartupId = it.StartupId,
                    ExpiresAt = it.ExpiresAt,
                    IsUsed = it.IsUsed
                })
                .ToListAsync(cancellationToken);

            return inviteTokens;
        }
    }
}
