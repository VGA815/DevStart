using DevStart.Application.Abstractions.Data;
using DevStart.Domain.StartupMembers;

namespace DevStart.Application.AccountDeletion
{
    /// <summary>
    /// Startups that would be left without a single founder if this user disappeared.
    ///
    /// They are erased along with the account, so the same rule has to answer two questions — "what am
    /// I about to lose?" (status query) and "what do I delete?" (eraser). Keeping it in one place is
    /// what stops those two answers from drifting apart.
    /// </summary>
    internal static class SoleFounderStartups
    {
        public static IQueryable<Guid> IdsFor(IApplicationDbContext context, Guid userId) =>
            context.StartupMembers
                .Where(m => m.ProfileId == userId && m.Role == StartupRole.Founder)
                .Where(m => !context.StartupMembers.Any(other =>
                    other.StartupId == m.StartupId
                    && other.Role == StartupRole.Founder
                    && other.ProfileId != userId))
                .Select(m => m.StartupId);
    }
}
