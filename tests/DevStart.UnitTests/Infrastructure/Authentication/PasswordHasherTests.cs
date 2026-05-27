using DevStart.Infrastructure.Authentication;
using Shouldly;

namespace DevStart.UnitTests.Infrastructure.Authentication;

public sealed class PasswordHasherTests
{
    private readonly PasswordHasher _sut = new();

    [Fact]
    public void Verify_CorrectPassword_ReturnsTrue()
    {
        string hash = _sut.Hash("S3cret!xx");

        _sut.Verify("S3cret!xx", hash).ShouldBeTrue();
    }

    [Fact]
    public void Verify_WrongPassword_ReturnsFalse()
    {
        string hash = _sut.Hash("S3cret!xx");

        _sut.Verify("not-the-password", hash).ShouldBeFalse();
    }

    [Theory]
    [InlineData("")]                 // empty stored hash
    [InlineData("nothexvalue")]      // no separator -> wrong part count
    [InlineData("no dash here")]     // no separator -> wrong part count
    [InlineData("AB-CD-EF")]         // too many parts
    [InlineData("ZZ-ZZ")]            // right shape but not valid hex
    public void Verify_MalformedStoredHash_ReturnsFalseWithoutThrowing(string malformed)
    {
        // A corrupt/legacy stored hash must fail verification gracefully rather than throw (which would
        // surface as an unhandled 500); this also protects the dummy-hash timing path in login.
        bool verified = Should.NotThrow(() => _sut.Verify("whatever", malformed));

        verified.ShouldBeFalse();
    }
}
