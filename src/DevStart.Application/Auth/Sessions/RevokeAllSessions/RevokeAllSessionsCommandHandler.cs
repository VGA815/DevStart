using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;
// The sibling Auth.RefreshToken namespace shadows the entity's simple name here.
using RefreshTokenEntity = DevStart.Domain.RefreshTokens.RefreshToken;

namespace DevStart.Application.Auth.Sessions.RevokeAllSessions
{
    internal sealed class RevokeAllSessionsCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IRefreshTokenService refreshTokenService,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<RevokeAllSessionsCommand>
    {
        public async Task<Result> Handle(RevokeAllSessionsCommand command, CancellationToken cancellationToken)
        {
            Guid userId = userContext.UserId;
            Guid? currentSessionId = userContext.SessionId;

            // "Everything, including me" is the credential-invalidation case, so it goes through the
            // shared path that also drops trusted devices.
            if (command.IncludeCurrent || currentSessionId is null)
            {
                await refreshTokenService.RevokeAllForUserAsync(userId, cancellationToken);
                return Result.Success();
            }

            DateTime now = dateTimeProvider.UtcNow;

            List<RefreshTokenEntity> others = await context.RefreshTokens
                .Where(t => t.UserId == userId && t.RevokedAt == null && t.SessionId != currentSessionId)
                .ToListAsync(cancellationToken);

            if (others.Count == 0)
            {
                return Result.Success();
            }

            foreach (RefreshTokenEntity token in others)
            {
                token.Revoke(now);
            }

            await context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }
}
