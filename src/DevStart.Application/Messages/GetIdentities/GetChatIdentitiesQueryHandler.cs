using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Messages.GetIdentities
{
    internal sealed class GetChatIdentitiesQueryHandler(IApplicationDbContext context, IUserContext userContext, IDateTimeProvider dateTimeProvider)
        : IQueryHandler<GetChatIdentitiesQuery, List<ChatIdentityResponse>>
    {
        public async Task<Result<List<ChatIdentityResponse>>> Handle(GetChatIdentitiesQuery query, CancellationToken cancellationToken)
        {
            List<Guid> startupIds = await StartupIdentity.ActableStartupIdsAsync(
                context, userContext.UserId, cancellationToken);

            if (startupIds.Count == 0)
            {
                return new List<ChatIdentityResponse>();
            }

            DateTime now = dateTimeProvider.UtcNow;

            // A banned startup cannot be spoken for, mirroring the public read filter.
            List<ChatIdentityResponse> identities = await context.Startups
                .AsNoTracking()
                .Where(s => startupIds.Contains(s.Id)
                         && !(s.IsBanned && (s.BanExpiresAt == null || s.BanExpiresAt > now)))
                .OrderBy(s => s.Name)
                .Select(s => new ChatIdentityResponse
                {
                    StartupId = s.Id,
                    Name = s.Name,
                    AvatarId = s.AvatarId,
                })
                .ToListAsync(cancellationToken);

            return identities;
        }
    }
}
