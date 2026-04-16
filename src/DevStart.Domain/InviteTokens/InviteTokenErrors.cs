using DevStart.SharedKernel;
using System;

namespace DevStart.Domain.InviteTokens
{
    public static class InviteTokenErrors
    {
        public static Error NotFound(Guid tokenId) => Error.NotFound(
            "InviteToken.NotFound",
            $"The invite token with the id = '{tokenId}' was not found.");
        public static readonly Error AlreadyUsed = Error.Problem(
            "InviteToken.AlreadyUsed",
            "The specified invite token is already used.");
        public static readonly Error Expired = Error.Problem(
            "InviteToken.Expired",
            "The specified invite token is already expired.");
    }
}
