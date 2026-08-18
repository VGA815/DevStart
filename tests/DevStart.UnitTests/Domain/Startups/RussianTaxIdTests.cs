using DevStart.Domain.Startups;
using Shouldly;

namespace DevStart.UnitTests.Domain.Startups;

public sealed class RussianTaxIdTests
{
    [Theory]
    [InlineData("7707083893")]   // organisation, 10 digits
    [InlineData("7736207543")]
    [InlineData("500100732259")] // sole trader, 12 digits
    [InlineData("  7707083893 ")]
    public void IsValidInn_ShouldAcceptCheckDigitMatch(string inn)
    {
        RussianTaxId.IsValidInn(inn).ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("7707083894")]    // last digit off by one — the typo this check exists to catch
    [InlineData("500100732258")]
    [InlineData("770708389")]     // 9 digits
    [InlineData("77070838931")]   // 11 digits
    [InlineData("77О708389З")]    // Cyrillic О and З where zeros and threes belong
    [InlineData("0000000000")]    // satisfies the checksum arithmetic; identifies nobody
    [InlineData("000000000000")]
    public void IsValidInn_ShouldRejectAnythingElse(string? inn)
    {
        RussianTaxId.IsValidInn(inn).ShouldBeFalse();
    }

    [Theory]
    [InlineData("1027700132195")]    // ОГРН, 13 digits
    [InlineData("304500116000157")]  // ОГРНИП, 15 digits
    public void IsValidOgrn_ShouldAcceptCheckDigitMatch(string ogrn)
    {
        RussianTaxId.IsValidOgrn(ogrn).ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1027700132196")]
    [InlineData("304500116000158")]
    [InlineData("102770013219")]
    [InlineData("1027700132195 7")]
    [InlineData("0000000000000")]
    public void IsValidOgrn_ShouldRejectAnythingElse(string? ogrn)
    {
        RussianTaxId.IsValidOgrn(ogrn).ShouldBeFalse();
    }

    [Fact]
    public void Normalize_ShouldKeepDigitsAndRejectMixedInput()
    {
        RussianTaxId.Normalize(" 7707083893 ").ShouldBe("7707083893");
        RussianTaxId.Normalize("7707-083893").ShouldBeNull();
        RussianTaxId.Normalize(null).ShouldBeNull();
    }

    [Fact]
    public void IsValidInn_ShouldNotImplyOwnership()
    {
        // The check digit says the number is well formed, nothing about who it belongs to. Someone
        // else's perfectly valid ИНН passes — which is why no wording downstream calls a match a
        // confirmation of ownership.
        RussianTaxId.IsValidInn("7707083893").ShouldBeTrue();
        RussianTaxId.IsValidInn("7736207543").ShouldBeTrue();
    }
}
