using DevStart.SharedKernel;

namespace DevStart.Domain.Security
{
    /// <summary>
    /// Per-user security policy. Deliberately its own table rather than a column on
    /// <c>user_preferences</c> (the client PUTs that row wholesale from the theme toggle and would
    /// clobber it) or on <c>user_two_factor</c> (that row is deleted when 2FA is disabled, and its
    /// concurrency token would make settings writes race the login code check).
    ///
    /// A missing row means defaults, so no backfill is needed — see <see cref="CreateDefault"/>.
    /// </summary>
    public sealed class UserSecuritySettings : Entity
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }

        public TwoFactorStrictness Strictness { get; set; }

        /// <summary>How long a device stays trusted, as chosen by the user. Clamped to the configured cap on use.</summary>
        public int TrustDurationDays { get; set; }

        public bool NotifyOnNewDeviceLogin { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public const int DefaultTrustDurationDays = 30;

        public UserSecuritySettings()
        {
        }

        public static UserSecuritySettings CreateDefault(Guid userId, DateTime now)
            => new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Strictness = TwoFactorStrictness.RememberDevice,
                TrustDurationDays = DefaultTrustDurationDays,
                NotifyOnNewDeviceLogin = true,
                CreatedAt = now,
                UpdatedAt = now,
            };

        /// <summary>
        /// Applies the new policy. Returns true when the trust policy itself changed, which is the
        /// signal to revoke the user's trusted devices — flipping only the email toggle must not.
        /// </summary>
        public bool Update(TwoFactorStrictness strictness, int trustDurationDays, bool notifyOnNewDeviceLogin, DateTime now)
        {
            bool trustPolicyChanged = Strictness != strictness || TrustDurationDays != trustDurationDays;

            Strictness = strictness;
            TrustDurationDays = trustDurationDays;
            NotifyOnNewDeviceLogin = notifyOnNewDeviceLogin;
            UpdatedAt = now;

            if (trustPolicyChanged)
            {
                Raise(new TrustedDevicesInvalidatedDomainEvent(UserId));
            }

            return trustPolicyChanged;
        }
    }
}
