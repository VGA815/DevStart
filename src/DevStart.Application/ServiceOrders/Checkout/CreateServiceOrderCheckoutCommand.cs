using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.ServiceOrders;

namespace DevStart.Application.ServiceOrders.Checkout
{
    /// <summary>Starts a one-time paid service purchase (SC-49). Returns a YooKassa confirmation URL.</summary>
    public sealed record CreateServiceOrderCheckoutCommand(ServiceType ServiceType)
        : ICommand<ServiceOrderCheckoutResponse>;

    public sealed class ServiceOrderCheckoutResponse
    {
        public Guid ServiceOrderId { get; init; }
        public Guid PaymentId { get; init; }
        public string? ConfirmationUrl { get; init; }
    }
}
