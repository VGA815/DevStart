using DevStart.Domain.Users;

namespace DevStart.Application.Abstractions.Authentication
{
    public interface ITokenProvider
    {
        /// <summary>
        /// <paramref name="sessionId"/> becomes the <c>sid</c> claim, letting an authenticated request
        /// say which session it belongs to (the sessions list marks it "current"). It is the refresh
        /// chain's root, so the claim stays valid across refreshes. Null on paths with no session.
        /// </summary>
        string CreateAccessToken(User user, Guid? sessionId = null);

        int AccessTokenLifetimeSeconds { get; }
    }
}
