using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;

namespace DevStart.Infrastructure.Authentication
{
    internal  static class ClaimPrincipalExtensions
    {
        public static Guid GetUserId(this ClaimsPrincipal? principal)
        {
            string? userId = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return Guid.TryParse(userId, out Guid parsedUserId) ?
                parsedUserId :
                throw new ApplicationException("User id is unavailable");
        }

        /// <summary>
        /// Null rather than throwing: access tokens minted before the claim existed are still valid
        /// until they expire, and callers only lose the "current session" marker.
        /// </summary>
        public static Guid? GetSessionId(this ClaimsPrincipal? principal)
        {
            string? sessionId = principal?.FindFirst(JwtRegisteredClaimNames.Sid)?.Value
                ?? principal?.FindFirst(ClaimTypes.Sid)?.Value;

            return Guid.TryParse(sessionId, out Guid parsed) ? parsed : null;
        }
    }
}
