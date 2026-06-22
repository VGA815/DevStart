using DevStart.SharedKernel;

namespace DevStart.Domain.Admin
{
    /// <summary>
    /// Immutable record of a privileged admin action (ban, subscription change, promo management, …)
    /// kept for audit. Written in the same transaction as the action it describes. A <c>null</c>
    /// <see cref="AdminUserId"/> denotes an automated/system action (e.g. the ban-expiry job).
    /// </summary>
    public sealed class AdminActionLog : Entity
    {
        public Guid Id { get; set; }
        public Guid? AdminUserId { get; set; }
        public AdminActionType ActionType { get; set; }
        public AdminTargetType TargetType { get; set; }
        public Guid TargetId { get; set; }
        public string Reason { get; set; } = null!;
        public string? MetadataJson { get; set; }
        public DateTime CreatedAt { get; set; }

        public AdminActionLog() { }

        public static AdminActionLog Create(
            Guid? adminUserId,
            AdminActionType actionType,
            AdminTargetType targetType,
            Guid targetId,
            string reason,
            DateTime createdAt,
            string? metadataJson = null)
            => new()
            {
                Id = Guid.NewGuid(),
                AdminUserId = adminUserId,
                ActionType = actionType,
                TargetType = targetType,
                TargetId = targetId,
                Reason = reason,
                MetadataJson = metadataJson,
                CreatedAt = createdAt,
            };
    }
}
