namespace DevStart.Application.Abstractions.Authentication
{
    public interface IEmailSender
    {
        Task SendVerification(string email, string token);

        /// <summary>
        /// Sends a "your Pro subscription is about to expire" reminder. Safe to call from a
        /// background job (does not depend on the current HTTP context).
        /// </summary>
        Task SendSubscriptionExpiring(string email, DateTime expiresAt);
    }
}
