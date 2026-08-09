using DevStart.Application.Configuration;
using DevStart.Domain.Users;

namespace DevStart.Application.Users.Security
{
    /// <summary>
    /// One place that answers "how long may this user trust a device for", so the query that renders
    /// the picker and the command that validates the choice can never disagree.
    /// </summary>
    internal static class SecurityTrustDuration
    {
        public static int CapFor(User user, TrustedDeviceOptions options)
        {
            int cap = user.Role == UserSystemRole.Admin ? options.AdminMaxTrustDays : options.MaxTrustDays;
            return Math.Max(cap, 1);
        }

        /// <summary>
        /// The presets at or below the cap. When the cap is below every preset (a deliberately tight
        /// admin ceiling, say), the cap itself is offered so the picker is never empty.
        /// </summary>
        public static IReadOnlyList<int> AvailableDurations(int cap)
        {
            int[] withinCap = [.. TrustedDeviceOptions.Presets.Where(d => d <= cap)];
            return withinCap.Length > 0 ? withinCap : [cap];
        }
    }
}
