using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Admin;

namespace DevStart.Application.Admin.Audit
{
    public sealed record GetAdminAuditLogQuery(
        AdminTargetType? TargetType = null,
        Guid? TargetId = null,
        int PageNumber = 1,
        int PageSize = 50) : IQuery<List<AdminAuditLogResponse>>;

    public sealed class AdminAuditLogResponse
    {
        public Guid Id { get; init; }
        public Guid? AdminUserId { get; init; }
        public AdminActionType ActionType { get; init; }
        public AdminTargetType TargetType { get; init; }
        public Guid TargetId { get; init; }
        public string Reason { get; init; } = null!;
        public string? MetadataJson { get; init; }
        public DateTime CreatedAt { get; init; }
    }
}
