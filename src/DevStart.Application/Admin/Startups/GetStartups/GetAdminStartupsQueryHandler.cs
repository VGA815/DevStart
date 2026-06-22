using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Startups;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Admin.Startups.GetStartups
{
    internal sealed class GetAdminStartupsQueryHandler(IApplicationDbContext context)
        : IQueryHandler<GetAdminStartupsQuery, List<AdminStartupListItemResponse>>
    {
        public async Task<Result<List<AdminStartupListItemResponse>>> Handle(
            GetAdminStartupsQuery query,
            CancellationToken cancellationToken)
        {
            IQueryable<Startup> startups = context.Startups.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                // Provider-agnostic case-insensitive contains (see note in GetUsersQueryHandler).
                string term = query.Search.Trim().ToLower();
                startups = startups.Where(s =>
                    s.Name.ToLower().Contains(term) || s.PublicEmail.ToLower().Contains(term));
            }
            if (query.IsBanned.HasValue)
            {
                startups = startups.Where(s => s.IsBanned == query.IsBanned.Value);
            }

            int pageSize = query.PageSize is > 0 and <= 200 ? query.PageSize : 50;
            int pageNumber = query.PageNumber > 0 ? query.PageNumber : 1;

            List<AdminStartupListItemResponse> items = await startups
                .OrderByDescending(s => s.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new AdminStartupListItemResponse
                {
                    Id = s.Id,
                    Name = s.Name,
                    PublicEmail = s.PublicEmail,
                    Stage = s.Stage,
                    IsStopped = s.IsStopped,
                    IsBanned = s.IsBanned,
                    BanReason = s.BanReason,
                    BannedAt = s.BannedAt,
                    BanExpiresAt = s.BanExpiresAt,
                    CreatedAt = s.CreatedAt,
                })
                .ToListAsync(cancellationToken);

            return items;
        }
    }
}
