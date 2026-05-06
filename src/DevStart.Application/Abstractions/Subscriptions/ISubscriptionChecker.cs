namespace DevStart.Application.Abstractions.Subscriptions
{
    /// <summary>
    /// Returns whether a user currently has an Active Pro subscription. Implementation is cached
    /// in Redis with a short TTL; cache is explicitly invalidated on activation/cancellation.
    /// </summary>
    public interface ISubscriptionChecker
    {
        Task<bool> HasActiveProAsync(Guid userId, CancellationToken ct);
    }
}
