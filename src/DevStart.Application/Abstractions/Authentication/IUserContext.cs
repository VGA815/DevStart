namespace DevStart.Application.Abstractions.Authentication
{
    public interface IUserContext
    {
        Guid UserId { get; }

        /// <summary>
        /// The refresh-chain root this access token was issued for (the <c>sid</c> claim), or null for
        /// tokens minted before the claim existed. Used to mark the caller's own row in the
        /// sessions list and to keep "revoke all" from cutting off the tab the user is looking at.
        /// </summary>
        Guid? SessionId { get; }
    }
}
