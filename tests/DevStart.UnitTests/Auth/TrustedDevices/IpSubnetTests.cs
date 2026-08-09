using DevStart.Infrastructure.Authentication.TrustedDevices;

namespace DevStart.UnitTests.Auth.TrustedDevices
{
    public class IpSubnetTests
    {
        [Theory]
        [InlineData("203.0.113.7", "203.0.113.200")]   // same /24
        [InlineData("10.0.0.1", "10.0.0.1")]
        [InlineData("2001:db8:1234::1", "2001:db8:1234:abcd::9")] // same /48
        [InlineData("203.0.113.7", "::ffff:203.0.113.9")]         // v4-mapped v6 normalizes to v4
        public void SameNetwork_IsTrue_ForAddressesInTheSamePrefix(string left, string right)
        {
            Assert.True(IpSubnet.SameNetwork(left, right));
        }

        [Theory]
        [InlineData("203.0.113.7", "203.0.114.7")]                 // different /24
        [InlineData("2001:db8:1234::1", "2001:db8:9999::1")]       // different /48
        [InlineData("203.0.113.7", "2001:db8::1")]                 // mixed family
        public void SameNetwork_IsFalse_ForDifferentNetworks(string left, string right)
        {
            Assert.False(IpSubnet.SameNetwork(left, right));
        }

        [Theory]
        [InlineData(null, "203.0.113.7")]
        [InlineData("203.0.113.7", null)]
        [InlineData("", "203.0.113.7")]
        [InlineData("not-an-ip", "203.0.113.7")]
        [InlineData("203.0.113.7", "still-not-an-ip")]
        public void SameNetwork_FailsClosed_OnMissingOrUnparsableInput(string? left, string? right)
        {
            // An ambiguous answer must cost a TOTP prompt, never grant the bypass.
            Assert.False(IpSubnet.SameNetwork(left, right));
        }
    }
}
