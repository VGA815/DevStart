namespace DevStart.Application.Abstractions.Authentication
{
    /// <summary>
    /// What the "new device signed in" email tells the user. A record rather than loose parameters so
    /// it can grow (city, device kind) without touching <see cref="IEmailSender"/>.
    /// </summary>
    public sealed record NewDeviceLoginInfo(
        string? Browser,
        string? Os,
        string? IpAddress,
        DateTime OccurredAtUtc);

    public interface IEmailSender
    {
        Task SendVerification(string email, string token);

        Task SendPasswordReset(string email, string token);

        /// <summary>
        /// Sends a "your Pro subscription is about to expire" reminder. Safe to call from a
        /// background job (does not depend on the current HTTP context).
        /// </summary>
        Task SendSubscriptionExpiring(string email, DateTime expiresAt);

        /// <summary>
        /// Warns about a sign-in from a browser/OS the account has not used recently. Runs from a
        /// background job, so it must not depend on the current HTTP context.
        /// </summary>
        Task SendNewDeviceLogin(string email, NewDeviceLoginInfo info);
    }
}
