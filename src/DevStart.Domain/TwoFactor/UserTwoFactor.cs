using DevStart.SharedKernel;

namespace DevStart.Domain.TwoFactor
{
    /// <summary>
    /// Per-user TOTP state (1:1 with users). A row is created in a pending state when the user
    /// starts enrollment and becomes active once the first code is confirmed. Disabling 2FA
    /// deletes the row (and cascades to recovery codes).
    /// </summary>
    public sealed class UserTwoFactor : Entity
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }

        /// <summary>AES-GCM-encrypted Base32 TOTP secret; never stored in plaintext.</summary>
        public string EncryptedSecret { get; set; } = null!;

        public bool IsEnabled { get; set; }
        public DateTime? EnabledAt { get; set; }

        /// <summary>
        /// The 30-second timestep of the last accepted TOTP code. Codes at or before this step are
        /// rejected so a sniffed code cannot be replayed within its validity window.
        /// </summary>
        public long? LastUsedTimestep { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public UserTwoFactor()
        {
        }

        public static UserTwoFactor CreatePending(Guid userId, string encryptedSecret, DateTime now)
            => new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                EncryptedSecret = encryptedSecret,
                IsEnabled = false,
                CreatedAt = now,
                UpdatedAt = now,
            };

        public Result RotatePendingSecret(string encryptedSecret, DateTime now)
        {
            if (IsEnabled)
            {
                return Result.Failure(TwoFactorErrors.AlreadyEnabled);
            }

            EncryptedSecret = encryptedSecret;
            UpdatedAt = now;
            return Result.Success();
        }

        public Result Enable(long confirmedTimestep, DateTime now)
        {
            if (IsEnabled)
            {
                return Result.Failure(TwoFactorErrors.AlreadyEnabled);
            }

            IsEnabled = true;
            EnabledAt = now;
            LastUsedTimestep = confirmedTimestep;
            UpdatedAt = now;

            Raise(new TwoFactorEnabledDomainEvent(UserId));
            return Result.Success();
        }

        /// <summary>
        /// Replay guard: accepts the timestep only if it is strictly newer than the last accepted
        /// one. Must be persisted in the same SaveChanges as the action it authorizes.
        /// </summary>
        public bool TryAcceptTimestep(long timestep, DateTime now)
        {
            if (LastUsedTimestep is not null && timestep <= LastUsedTimestep)
            {
                return false;
            }

            LastUsedTimestep = timestep;
            UpdatedAt = now;
            return true;
        }
    }
}
