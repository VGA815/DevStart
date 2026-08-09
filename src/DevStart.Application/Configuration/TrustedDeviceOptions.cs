namespace DevStart.Application.Configuration
{
    /// <summary>
    /// Tunables for the "remember this device" second-factor bypass. Lives in Application (not
    /// Infrastructure, where <c>TwoFactorOptions</c> and <c>RefreshTokenOptions</c> sit) because the
    /// login gate consumes it and Application must not reference Infrastructure. Bound from the
    /// "TrustedDevices" section by Infrastructure, the same way <c>ValuationOptions</c> is.
    /// </summary>
    public sealed class TrustedDeviceOptions
    {
        public const string SectionName = "TrustedDevices";

        /// <summary>Global kill switch: when false, device tokens are never minted or honoured.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>Upper bound on the user's chosen trust duration.</summary>
        public int MaxTrustDays { get; set; } = 30;

        /// <summary>Shorter bound for admins, whose accounts are the highest-value target.</summary>
        public int AdminMaxTrustDays { get; set; } = 7;

        /// <summary>Above this, minting a device evicts the least recently used one.</summary>
        public int MaxDevicesPerUser { get; set; } = 10;

        /// <summary>The durations offered in the UI; the effective list is filtered by the applicable cap.</summary>
        public static readonly int[] Presets = [7, 14, 30, 60, 90];
    }
}
