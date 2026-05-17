using DevStart.Application.Abstractions.Authentication;
using DevStart.Infrastructure.Authentication.OAuth;
using System.Security.Cryptography;
using System.Text;

namespace DevStart.UnitTests.Auth.OAuth
{
    public class PkceGeneratorTests
    {
        private readonly PkceGenerator _sut = new();

        [Fact]
        public void Create_VerifierLength_IsWithinRfc7636Bounds()
        {
            PkcePair pair = _sut.Create();

            Assert.InRange(pair.Verifier.Length, 43, 128);
        }

        [Fact]
        public void Create_Challenge_IsBase64UrlSha256OfVerifier()
        {
            PkcePair pair = _sut.Create();

            byte[] expected = SHA256.HashData(Encoding.ASCII.GetBytes(pair.Verifier));
            string expectedChallenge = Convert.ToBase64String(expected)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');

            Assert.Equal(expectedChallenge, pair.Challenge);
        }

        [Fact]
        public void Create_Verifier_HasNoPaddingOrUnsafeChars()
        {
            PkcePair pair = _sut.Create();

            Assert.DoesNotContain('=', pair.Verifier);
            Assert.DoesNotContain('+', pair.Verifier);
            Assert.DoesNotContain('/', pair.Verifier);
        }

        [Fact]
        public void Create_ProducesUniquePairs()
        {
            PkcePair a = _sut.Create();
            PkcePair b = _sut.Create();

            Assert.NotEqual(a.Verifier, b.Verifier);
            Assert.NotEqual(a.Challenge, b.Challenge);
        }
    }
}
