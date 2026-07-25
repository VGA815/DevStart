using DevStart.SharedKernel;

namespace DevStart.Domain.ServiceOrders
{
    /// <summary>Raised when a one-time service order's payment is captured (once, on the real transition).</summary>
    public sealed record ServiceOrderPaidDomainEvent(
        Guid ServiceOrderId,
        Guid UserId,
        ServiceType ServiceType) : IDomainEvent;
}
