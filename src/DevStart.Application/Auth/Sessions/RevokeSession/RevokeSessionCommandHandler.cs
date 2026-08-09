using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.RefreshTokens;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;
// The sibling Auth.RefreshToken namespace shadows the entity's simple name here.
using RefreshTokenEntity = DevStart.Domain.RefreshTokens.RefreshToken;

namespace DevStart.Application.Auth.Sessions.RevokeSession
{
    internal sealed class RevokeSessionCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<RevokeSessionCommand>
    {
        public async Task<Result> Handle(RevokeSessionCommand command, CancellationToken cancellationToken)
        {
            Guid userId = userContext.UserId;
            DateTime now = dateTimeProvider.UtcNow;

            // Scoped to the caller, and NotFound (never Forbidden) for someone else's session id, so
            // the endpoint can't be used to probe which session ids exist.
            List<RefreshTokenEntity> chain = await context.RefreshTokens
                .Where(t => t.UserId == userId && t.SessionId == command.SessionId)
                .ToListAsync(cancellationToken);

            if (chain.Count == 0)
            {
                return Result.Failure(RefreshTokenErrors.SessionNotFound);
            }

            bool changed = false;
            foreach (RefreshTokenEntity token in chain.Where(t => t.RevokedAt is null))
            {
                token.Revoke(now);
                changed = true;
            }

            if (changed)
            {
                await context.SaveChangesAsync(cancellationToken);
            }

            return Result.Success();
        }
    }
}
