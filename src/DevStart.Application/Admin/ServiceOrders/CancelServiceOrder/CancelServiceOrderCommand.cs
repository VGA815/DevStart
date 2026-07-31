using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Admin.ServiceOrders.CancelServiceOrder
{
    /// <summary>
    /// Administratively closes a one-time service order and takes back what it delivered. This does not
    /// move money — a paid order is refunded through <c>api/payments/{paymentId}/refund</c>, which
    /// cancels the order on its own; this command is for orders that cannot or should not be delivered.
    /// </summary>
    public sealed record CancelServiceOrderCommand(Guid ServiceOrderId, string Reason) : ICommand;
}
