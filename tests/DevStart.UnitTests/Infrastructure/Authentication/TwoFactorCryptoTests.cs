using DevStart.Infrastructure.Authentication.TwoFactor;
using DevStart.UnitTests.TestSupport;

namespace DevStart.UnitTests.Infrastructure.Authentication
{
    public class TotpProviderTests
    {
        private readonly TotpProvider _sut = TwoFactorTestKit.CreateTotpProvider();

        [Fact]
        public void GenerateSecret_ProducesBase32Of20Bytes()
        {
            string secret = _sut.GenerateSecret();

            // 20 bytes -> 32 Base32 chars, no padding.
            Assert.Equal(32, secret.Length);
            Assert.True(secret.All(c => char.IsAsciiLetterUpper(c) || (c >= '2' && c <= '7')));
        }

        [Fact]
        public void VerifyCode_AcceptsCurrentCode_AndReportsTimestep()
        {
            string secret = _sut.GenerateSecret();
            string code = TwoFactorTestKit.CurrentCodeFor(secret);

            Assert.True(_sut.VerifyCode(secret, code, lastUsedTimestep: null, out long timestep));
            Assert.True(timestep > 0);
        }

        [Fact]
        public void VerifyCode_AcceptsPreviousStep_WithinDriftWindow()
        {
            string secret = _sut.GenerateSecret();
            string previous = TwoFactorTestKit.CurrentCodeFor(secret, stepOffset: -1);

            Assert.True(_sut.VerifyCode(secret, previous, lastUsedTimestep: null, out _));
        }

        [Fact]
        public void VerifyCode_RejectsReplayedTimestep()
        {
            string secret = _sut.GenerateSecret();
            string code = TwoFactorTestKit.CurrentCodeFor(secret);

            Assert.True(_sut.VerifyCode(secret, code, lastUsedTimestep: null, out long timestep));
            Assert.False(_sut.VerifyCode(secret, code, lastUsedTimestep: timestep, out _));
        }

        [Fact]
        public void VerifyCode_RejectsWrongCode()
        {
            string secret = _sut.GenerateSecret();

            Assert.False(_sut.VerifyCode(secret, TwoFactorTestKit.WrongCodeFor(secret), lastUsedTimestep: null, out _));
        }

        [Fact]
        public void BuildOtpAuthUri_ContainsSecretIssuerAndParams()
        {
            string secret = _sut.GenerateSecret();
            string uri = _sut.BuildOtpAuthUri(secret, "user@example.com");

            Assert.StartsWith("otpauth://totp/", uri);
            Assert.Contains($"secret={secret}", uri);
            Assert.Contains("issuer=DevStart-Tests", uri);
            Assert.Contains("algorithm=SHA1", uri);
            Assert.Contains("digits=6", uri);
            Assert.Contains("period=30", uri);
        }
    }

    public class AesGcmTwoFactorSecretProtectorTests
    {
        [Fact]
        public void ProtectUnprotect_RoundTrips()
        {
            AesGcmTwoFactorSecretProtector sut = TwoFactorTestKit.CreateProtector();
            string secret = "JBSWY3DPEHPK3PXPJBSWY3DPEHPK3PXP";

            string encrypted = sut.Protect(secret);

            Assert.NotEqual(secret, encrypted);
            Assert.DoesNotContain(secret, encrypted);
            Assert.StartsWith("v1:", encrypted);
            Assert.Equal(secret, sut.Unprotect(encrypted));
        }

        [Fact]
        public void Protect_UsesFreshNoncePerCall()
        {
            AesGcmTwoFactorSecretProtector sut = TwoFactorTestKit.CreateProtector();

            Assert.NotEqual(sut.Protect("SECRET"), sut.Protect("SECRET"));
        }
    }

    public class RecoveryCodeGeneratorTests
    {
        private readonly RecoveryCodeGenerator _sut = TwoFactorTestKit.CreateRecoveryCodeGenerator();

        [Fact]
        public void Generate_ProducesDistinctFormattedCodes()
        {
            IReadOnlyList<string> codes = _sut.Generate(10);

            Assert.Equal(10, codes.Count);
            Assert.Equal(10, codes.Distinct().Count());
            Assert.All(codes, c => Assert.Matches("^[0-9A-Z]{4}-[0-9A-Z]{4}-[0-9A-Z]{2}$", c));
        }

        [Fact]
        public void Hash_IsCaseAndDashInsensitive()
        {
            string code = _sut.Generate(1).Single();

            Assert.Equal(_sut.Hash(code), _sut.Hash(code.ToLowerInvariant().Replace("-", "")));
        }
    }
}
