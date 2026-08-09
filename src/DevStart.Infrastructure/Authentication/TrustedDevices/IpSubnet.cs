using System.Net;
using System.Net.Sockets;

namespace DevStart.Infrastructure.Authentication.TrustedDevices
{
    /// <summary>
    /// Coarse "same network?" test backing <see cref="Domain.Security.TwoFactorStrictness.SameNetworkOnly"/>.
    /// Fails closed: anything unparsable, missing, or of a different address family counts as a
    /// different network, so an ambiguous answer costs the user a TOTP prompt rather than the bypass.
    /// </summary>
    internal static class IpSubnet
    {
        private const int IPv4PrefixBits = 24;
        private const int IPv6PrefixBits = 48;

        public static bool SameNetwork(string? left, string? right)
        {
            if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
            {
                return false;
            }

            if (!IPAddress.TryParse(left, out IPAddress? a) || !IPAddress.TryParse(right, out IPAddress? b))
            {
                return false;
            }

            a = Normalize(a);
            b = Normalize(b);

            if (a.AddressFamily != b.AddressFamily)
            {
                return false;
            }

            int prefixBits = a.AddressFamily == AddressFamily.InterNetwork ? IPv4PrefixBits : IPv6PrefixBits;

            return SamePrefix(a.GetAddressBytes(), b.GetAddressBytes(), prefixBits);
        }

        // ::ffff:203.0.113.7 and 203.0.113.7 are the same host; compare them as IPv4.
        private static IPAddress Normalize(IPAddress address)
            => address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;

        private static bool SamePrefix(byte[] a, byte[] b, int prefixBits)
        {
            if (a.Length != b.Length)
            {
                return false;
            }

            int wholeBytes = prefixBits / 8;
            for (int i = 0; i < wholeBytes; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }

            int remainingBits = prefixBits % 8;
            if (remainingBits == 0)
            {
                return true;
            }

            int mask = 0xFF << (8 - remainingBits);
            return (a[wholeBytes] & mask) == (b[wholeBytes] & mask);
        }
    }
}
