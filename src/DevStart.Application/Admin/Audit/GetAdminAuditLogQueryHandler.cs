using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Admin;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Admin.Audit
{
    internal sealed class GetAdminAuditLogQueryHandler(IApplicationDbContext context)
        : IQueryHandler<GetAdminAuditLogQuery, List<AdminAuditLogResponse>>
    {
        public async Task<Result<List<AdminAuditLogResponse>>> Handle(
            GetAdminAuditLogQuery query,
            CancellationToken cancellationToken)
        {
            IQueryable<AdminActionLog> logs = context.AdminActionLogs.AsNoTracking();

            if (query.TargetType.HasValue)
            {
                logs = logs.Where(l => l.TargetType == query.TargetType.Value);
            }
            if (query.TargetId.HasValue)
            {
                logs = logs.Where(l => l.TargetId == query.TargetId.Value);
            }

            int pageSize = query.PageSize is > 0 and <= 200 ? query.PageSize : 50;
            int pageNumber = query.PageNumber > 0 ? query.PageNumber : 1;

            List<AdminAuditLogResponse> items = await logs
                .OrderByDescending(l => l.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new AdminAuditLogResponse
                {
                    Id = l.Id,
                    AdminUserId = l.AdminUserId,
                    ActionType = l.ActionType,
                    TargetType = l.TargetType,
                    TargetId = l.TargetId,
                    Reason = l.Reason,
                    MetadataJson = l.MetadataJson,
                    CreatedAt = l.CreatedAt,
                })
                .ToListAsync(cancellationToken);

            return items;
        }
    }
}
