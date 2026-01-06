using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.Startups;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Startups.GetById
{
    internal sealed class GetStartupByIdQueryHandler(IApplicationDbContext context)
        : IQueryHandler<GetStartupByIdQuery, StartupResponse>
    {
        public async Task<Result<StartupResponse>> Handle(GetStartupByIdQuery query, CancellationToken cancellationToken)
        {
            StartupResponse? startup = await context.Startups
                .Where(s => s.Id == query.StartupId)
                .Select(s => new StartupResponse
                {
                    Id = s.Id,
                    AvatarUrl = s.AvatarUrl,
                    BillingEmail = s.BillingEmail,
                    CreatedAt = s.CreatedAt,
                    Description = s.Description,
                    IsStopped = s.IsStopped,
                    Location = s.Location,
                    Name = s.Name,
                    PublicEmail = s.PublicEmail,
                    SocialMediaLinks = s.SocialMediaLinks,
                    Stage = s.Stage,
                    UpdatedAt = s.UpdatedAt,
                    Url = s.Url,
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
