using DevStart.Application.Abstractions.Data;
using DevStart.Domain.Messages;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Messages
{
    /// <summary>
    /// Shared read-access rule for a single message: the acting user is either one of its two
    /// participants directly, or a member of a startup that participates in it.
    /// </summary>
    internal static class MessageAccess
    {
        public static async Task<bool> CanReadAsync(
            IApplicationDbContext context,
            Message message,
            Guid userId,
            CancellationToken cancellationToken)
        {
            bool isDirectUser =
                (message.SenderType == ChatParticipantType.User && message.SenderId == userId) ||
                (message.ReceiverType == ChatParticipantType.User && message.ReceiverId == userId);

            if (isDirectUser)
            {
                return true;
            }

            var startupIds = new List<Guid>(2);
            if (message.SenderType == ChatParticipantType.Startup) startupIds.Add(message.SenderId);
            if (message.ReceiverType == ChatParticipantType.Startup) startupIds.Add(message.ReceiverId);

            if (startupIds.Count == 0)
            {
                return false;
            }

            return await context.StartupMembers.AnyAsync(
                sm => sm.ProfileId == userId && startupIds.Contains(sm.StartupId),
                cancellationToken);
        }
    }
}
