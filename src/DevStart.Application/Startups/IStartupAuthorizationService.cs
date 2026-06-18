namespace DevStart.Application.Startups
{
    /// <summary>
    /// Authorization helper for startup-scoped actions, removing the repeated founder/administrator
    /// membership lookups across handlers.
    /// </summary>
    public interface IStartupAuthorizationService
    {
        Task<bool> IsFounderOrAdminAsync(Guid userId, Guid startupId, CancellationToken cancellationToken);
    }
}
