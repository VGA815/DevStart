using DevStart.SharedKernel;

namespace DevStart.Domain.ServiceOrders
{
    /// <summary>
    /// Raised when a one-time service order's payment is captured (once, on the real transition).
    /// Carries <paramref name="TargetId"/> so the fulfillment handler knows what to deliver against
    /// without re-reading the order.
    /// </summary>
    public sealed record ServiceOrderPaidDomainEvent(
        Guid ServiceOrderId,
        Guid UserId,
        ServiceType ServiceType,
        Guid? TargetId) : IDomainEvent;
}
