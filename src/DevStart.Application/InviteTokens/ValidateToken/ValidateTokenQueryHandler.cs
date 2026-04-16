using Microsoft.EntityFrameworkCore;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.InviteTokens;
using DevStart.SharedKernel;

namespace DevStart.Application.InviteTokens.ValidateToken
{
    internal sealed class ValidateTokenQueryHandler(IApplicationDbContext context, IDateTimeProvider dateTimeProvider)
        : IQueryHandler<ValidateTokenQuery, bool>
    {
        public async Task<Result<bool>> Handle(ValidateTokenQuery query, CancellationToken cancellationToken)
        {
            InviteToken? inviteToken = await context.InviteTokens.SingleOrDefaultAsync(t => t.Id == query.TokenId, cancellationToken);

            if (inviteToken == null || inviteToken.IsUsed || inviteToken.ExpiresAt < dateTimeProvider.UtcNow)
            {
                return false;
            }

            return true;
        }
    }
}
