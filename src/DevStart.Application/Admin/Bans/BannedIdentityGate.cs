using DevStart.Application.Abstractions.Data;
using DevStart.Domain.Admin;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Admin.Bans
{
    /// <summary>
    /// Guards the two doors a new account can come through (password sign-up and OAuth) against an
    /// address whose account was banned and then erased. Without it, "delete my account" would clear
    /// the ban along with the row it lives on — see <see cref="BannedIdentity"/>.
    /// </summary>
    internal static class BannedIdentityGate
    {
        public static async Task<bool> IsBarredAsync(
            IApplicationDbContext context,
            string email,
            DateTime utcNow,
            CancellationToken cancellationToken)
        {
            string emailHash = BannedIdentity.HashEmail(email);

            return await context.BannedIdentities.AnyAsync(
                b => b.EmailHash == emailHash && (b.BanExpiresAt == null || b.BanExpiresAt > utcNow),
                cancellationToken);
        }
    }
}
