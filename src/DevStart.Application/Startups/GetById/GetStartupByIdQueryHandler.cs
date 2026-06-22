using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Startups;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Startups.GetById
{
    internal sealed class GetStartupByIdQueryHandler(IApplicationDbContext context, IDateTimeProvider dateTimeProvider)
        : IQueryHandler<GetStartupByIdQuery, StartupResponse>
    {
        public async Task<Result<StartupResponse>> Handle(GetStartupByIdQuery query, CancellationToken cancellationToken)
        {
            // Hide a currently-banned startup; honour lazy expiry of temporary bans.
            DateTime now = dateTimeProvider.UtcNow;
            StartupResponse? startup = await context.Startups
                .Where(s => s.Id == query.StartupId
                         && !(s.IsBanned && (s.BanExpiresAt == null || s.BanExpiresAt > now)))
                .Select(s => new StartupResponse
                {
                    Id = s.Id,
                    AvatarId = s.AvatarId,
                    BillingEmail = s.BillingEmail,
                    CreatedAt = s.CreatedAt,
                    Description = s.Description,
                    IsStopped = s.IsStopped,
                    ShortDescription = s.ShortDescription,
                    Location = s.Location,
                    Name = s.Name,
                    PublicEmail = s.PublicEmail,
                    SocialMediaLinks = s.SocialMediaLinks,
                    Stage = s.Stage,
                    UpdatedAt = s.UpdatedAt,
                    Url = s.Url,
                    Tam = s.Tam,
                    Sam = s.Sam,
                    Som = s.Som,
                    MarketGrowthRate = s.MarketGrowthRate,
                    HasPatents = s.HasPatents,
                })
                .SingleOrDefaultAsync(cancellationToken);

            if (startup == null)
            {
                return Result.Failure<StartupResponse>(StartupErrors.NotFound(query.StartupId));
            }

            return startup;
        }
    }
}
