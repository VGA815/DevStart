using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;
// The sibling Auth.RefreshToken namespace shadows the entity's simple name here.
using RefreshTokenEntity = DevStart.Domain.RefreshTokens.RefreshToken;

namespace DevStart.Application.Auth.Sessions.GetSessions
{
    internal sealed class GetSessionsQueryHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IDateTimeProvider dateTimeProvider)
        : IQueryHandler<GetSessionsQuery, IReadOnlyList<SessionResponse>>
    {
        public async Task<Result<IReadOnlyList<SessionResponse>>> Handle(
            GetSessionsQuery query, CancellationToken cancellationToken)
        {
            Guid userId = userContext.UserId;
            Guid? currentSessionId = userContext.SessionId;
            DateTime now = dateTimeProvider.UtcNow;

            // Exactly one row per chain is ever active (rotation revokes its predecessor), so the
            // active rows are already one-per-session — no grouping needed.
            List<RefreshTokenEntity> active = await context.RefreshTokens
                .AsNoTracking()
                .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > now)
                .OrderByDescending(t => t.LastUsedAt)
                .ToListAsync(cancellationToken);

            IReadOnlyList<SessionResponse> sessions = [.. active.Select(t =>
            {
                UserAgentInfo ua = UserAgentParser.Parse(t.UserAgent);
                return new SessionResponse(
                    t.SessionId,
                    t.SessionId == currentSessionId,
                    t.SessionStartedAt,
                    t.LastUsedAt,
                    t.ExpiresAt,
                    t.CreatedByIp,
                    ua.Browser,
                    ua.Os,
                    ua.Kind.ToString());
            })];

            return Result.Success(sessions);
        }
    }
}
