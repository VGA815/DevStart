using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.ServiceOrders;

namespace DevStart.Application.Admin.ServiceOrders.GetServiceOrders
{
    public sealed record GetAdminServiceOrdersQuery(
        Guid? UserId = null,
        ServiceOrderStatus? Status = null,
        ServiceType? ServiceType = null,
        int PageNumber = 1,
        int PageSize = 50) : IQuery<List<AdminServiceOrderResponse>>;

    public sealed class AdminServiceOrderResponse
    {
        public Guid Id { get; init; }
        public Guid UserId { get; init; }
        public string? UserEmail { get; init; }
        public ServiceType ServiceType { get; init; }
        public ServiceTargetKind TargetKind { get; init; }
        public Guid? TargetId { get; init; }
        public decimal Amount { get; init; }
        public string Currency { get; init; } = "RUB";
        public ServiceOrderStatus Status { get; init; }
        public bool IsActive { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime? PaidAt { get; init; }
        public DateTime? FulfilledAt { get; init; }
        public DateTime? ExpiresAt { get; init; }
        public DateTime? CancelledAt { get; init; }
        public DateTime? RefundedAt { get; init; }
    }
}
