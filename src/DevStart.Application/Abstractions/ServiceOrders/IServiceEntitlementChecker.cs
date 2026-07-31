using DevStart.Domain.ServiceOrders;

namespace DevStart.Application.Abstractions.ServiceOrders
{
    /// <summary>
    /// Returns whether a user currently holds a paid one-time service entitlement (SC-49) for a given
    /// target — the per-purchase counterpart of <see cref="Subscriptions.ISubscriptionChecker"/>.
    /// Implementation is cached in Redis with a TTL clamped to the remaining access window, and the
    /// cache is invalidated when an order is fulfilled, refunded or cancelled.
    /// </summary>
    public interface IServiceEntitlementChecker
    {
        Task<bool> HasAsync(Guid userId, ServiceType serviceType, Guid targetId, CancellationToken ct);

        /// <summary>Drops every cached entitlement answer for a user after their orders change.</summary>
        Task InvalidateAsync(Guid userId, CancellationToken ct);
    }
}
