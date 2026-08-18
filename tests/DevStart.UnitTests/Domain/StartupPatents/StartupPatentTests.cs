using DevStart.Domain.StartupPatents;
using Shouldly;

namespace DevStart.UnitTests.Domain.StartupPatents;

public sealed class StartupPatentTests
{
    private const int CurrentYear = 2026;

    [Fact]
    public void Create_ShouldKeepRawNumberAndNormalizedKey()
    {
        Guid startupId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        DateTime createdAt = new(2026, 8, 17, 10, 0, 0, DateTimeKind.Utc);

        StartupPatent patent = StartupPatent.Create(
            startupId,
            IntellectualPropertyKind.Invention,
            "  RU 2 731 234 C1  ",
            StartupPatent.NormalizeNumber("RU 2 731 234 C1")!,
            createdAt);

        patent.Id.ShouldNotBe(Guid.Empty);
        patent.StartupId.ShouldBe(startupId);
        patent.Kind.ShouldBe(IntellectualPropertyKind.Invention);

        // The founder recognises what they typed; the register is searched by the digits.
        patent.NumberRaw.ShouldBe("RU 2 731 234 C1");
        patent.NumberNormalized.ShouldBe("2731234");
        patent.CreatedAt.ShouldBe(createdAt);
    }

    [Theory]
    [InlineData("2731234", "2731234")]
    [InlineData("RU 2 731 234 C1", "2731234")]
    [InlineData("RU2731234C1", "2731234")]
    [InlineData("№ 2731234", "2731234")]
    [InlineData("RU №2731234", "2731234")]
    [InlineData("2 023 612 345", "2023612345")]
    [InlineData("2023612345 ", "2023612345")]
    public void NormalizeNumber_ShouldReduceDecorationToDigits(string raw, string expected)
    {
        StartupPatent.NormalizeNumber(raw).ShouldBe(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("патент есть")]
    [InlineData("2731234 и ещё один")]
    public void NormalizeNumber_ShouldReturnNull_WhenNotComparable(string? raw)
    {
        // Null means "not comparable", never "duplicate of every other null" — the same rule the
        // competitor cards' domain normalization follows.
        StartupPatent.NormalizeNumber(raw).ShouldBeNull();
    }

    [Theory]
    [InlineData(IntellectualPropertyKind.Invention, "2731234", true)]
    [InlineData(IntellectualPropertyKind.Invention, "273123", false)]
    [InlineData(IntellectualPropertyKind.Invention, "27312345", false)]
    [InlineData(IntellectualPropertyKind.UtilityModel, "213456", true)]
    [InlineData(IntellectualPropertyKind.UtilityModel, "2134", false)]
    [InlineData(IntellectualPropertyKind.IndustrialDesign, "132456", true)]
    [InlineData(IntellectualPropertyKind.Trademark, "812345", true)]
    [InlineData(IntellectualPropertyKind.ComputerProgram, "2023612345", true)]
    [InlineData(IntellectualPropertyKind.ComputerProgram, "202361234", false)]
    [InlineData(IntellectualPropertyKind.ComputerProgram, "2023712345", false)]
    [InlineData(IntellectualPropertyKind.ComputerProgram, "1899612345", false)]
    [InlineData(IntellectualPropertyKind.Database, "2023621234", true)]
    public void IsNumberWellFormed_ShouldCheckShapeAgainstKind(
        IntellectualPropertyKind kind, string normalized, bool expected)
    {
        StartupPatent.IsNumberWellFormed(kind, normalized, CurrentYear).ShouldBe(expected);
    }

    [Fact]
    public void IsNumberWellFormed_ShouldRejectCertificateYearBeyondNextYear()
    {
        // A certificate cannot be issued two years from now; a year in the future by one is allowed,
        // because the register runs slightly ahead of the calendar at a year boundary.
        StartupPatent.IsNumberWellFormed(
            IntellectualPropertyKind.ComputerProgram, "2027612345", CurrentYear).ShouldBeTrue();
        StartupPatent.IsNumberWellFormed(
            IntellectualPropertyKind.ComputerProgram, "2028612345", CurrentYear).ShouldBeFalse();
    }

    [Fact]
    public void IsNumberWellFormed_ShouldNotSeparateProgramsFromDatabases()
    {
        // Deliberate: the digit that tells a program from a database is the register's business. A
        // mis-picked kind must surface as "not found in the register", not as a format error, because
        // those two statements mean different things to the founder reading them.
        StartupPatent.IsNumberWellFormed(
            IntellectualPropertyKind.Database, "2023612345", CurrentYear).ShouldBeTrue();
        StartupPatent.IsNumberWellFormed(
            IntellectualPropertyKind.ComputerProgram, "2023621234", CurrentYear).ShouldBeTrue();
    }

    [Fact]
    public void IsNumberWellFormed_ShouldRejectNullOrEmpty()
    {
        StartupPatent.IsNumberWellFormed(IntellectualPropertyKind.Invention, null, CurrentYear).ShouldBeFalse();
        StartupPatent.IsNumberWellFormed(IntellectualPropertyKind.Invention, "", CurrentYear).ShouldBeFalse();
    }
}
