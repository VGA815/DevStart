using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.ServiceOrders;

namespace DevStart.Application.ServiceOrders.GetMine
{
    /// <summary>
    /// The caller's one-time service orders (SC-49). Not cacheable: it is scoped to the current user
    /// and has to reflect a just-captured payment immediately.
    /// </summary>
    public sealed record GetMyServiceOrdersQuery() : IQuery<List<ServiceOrderResponse>>;

    public sealed class ServiceOrderResponse
    {
        public Guid Id { get; init; }
        public ServiceType ServiceType { get; init; }
        public ServiceTargetKind TargetKind { get; init; }
        public Guid? TargetId { get; init; }

        /// <summary>Display name of the target, when it has one (a startup). Null for deals.</summary>
        public string? TargetName { get; init; }

        public decimal Amount { get; init; }
        public string Currency { get; init; } = "RUB";
        public ServiceOrderStatus Status { get; init; }

        /// <summary>Whether the order currently entitles the buyer to the service.</summary>
        public bool IsActive { get; init; }

        public DateTime CreatedAt { get; init; }
        public DateTime? PaidAt { get; init; }
        public DateTime? FulfilledAt { get; init; }
        public DateTime? ExpiresAt { get; init; }
    }
}
