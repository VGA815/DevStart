using DevStart.Infrastructure.Authentication;

namespace DevStart.UnitTests.Auth.Sessions
{
    public class SessionRetentionPolicyTests
    {
        [Fact]
        public void RefreshTokenRetention_OutlivesTheKnownDeviceLookback()
        {
            // The new-device email decides "have we seen this browser?" from the user's own
            // refresh_tokens history. If cleanup started deleting rows the lookback still wants,
            // every user would get "new device" warnings for the browser they use every day.
            Assert.True(
                SessionRetentionPolicy.RefreshTokenRetention > SessionRetentionPolicy.KnownDeviceLookback,
                $"Refresh tokens are kept {SessionRetentionPolicy.RefreshTokenRetention.TotalDays} days but the " +
                $"new-device lookback reaches back {SessionRetentionPolicy.KnownDeviceLookback.TotalDays} days.");
        }

        [Fact]
        public void EveryRetentionWindow_IsPositive()
        {
            Assert.True(SessionRetentionPolicy.KnownDeviceLookback > TimeSpan.Zero);
            Assert.True(SessionRetentionPolicy.RefreshTokenRetention > TimeSpan.Zero);
            Assert.True(SessionRetentionPolicy.TrustedDeviceRetention > TimeSpan.Zero);
        }
    }
}
