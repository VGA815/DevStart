using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Admin.Users.GetUsers
{
    internal sealed class GetUsersQueryHandler(IApplicationDbContext context)
        : IQueryHandler<GetUsersQuery, List<AdminUserListItemResponse>>
    {
        public async Task<Result<List<AdminUserListItemResponse>>> Handle(
            GetUsersQuery query,
            CancellationToken cancellationToken)
        {
            IQueryable<User> users = context.Users.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                // Provider-agnostic case-insensitive contains. A PostgreSQL ILIKE/citext optimisation would
                // require leaking the Npgsql provider into the Application layer, so it's kept out here; a
                // case-insensitive index belongs in the Infrastructure mapping if this ever gets hot.
                string term = query.Search.Trim().ToLower();
                users = users.Where(u =>
                    u.Email.ToLower().Contains(term) || u.Username.ToLower().Contains(term));
            }
            if (query.Role.HasValue)
            {
                users = users.Where(u => u.Role == query.Role.Value);
            }
            if (query.IsBanned.HasValue)
            {
                users = users.Where(u => u.IsBanned == query.IsBanned.Value);
            }

            int pageSize = query.PageSize is > 0 and <= 200 ? query.PageSize : 50;
            int pageNumber = query.PageNumber > 0 ? query.PageNumber : 1;

            List<AdminUserListItemResponse> items = await users
                .OrderByDescending(u => u.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new AdminUserListItemResponse
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    Role = u.Role,
                    IsVerified = u.IsVerified,
                    IsBanned = u.IsBanned,
                    BanReason = u.BanReason,
                    BannedAt = u.BannedAt,
                    BanExpiresAt = u.BanExpiresAt,
                    CreatedAt = u.CreatedAt,
                })
                .ToListAsync(cancellationToken);

            return items;
        }
    }
}
