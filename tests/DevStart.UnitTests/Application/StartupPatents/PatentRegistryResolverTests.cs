using DevStart.Application.StartupPatents;
using DevStart.Domain.PatentRegistry;
using DevStart.Domain.Registries;
using DevStart.Domain.StartupPatents;
using DevStart.Domain.Startups;
using DevStart.Infrastructure.Database;
using DevStart.UnitTests.TestSupport;
using Shouldly;

namespace DevStart.UnitTests.Application.StartupPatents;

public sealed class PatentRegistryResolverTests
{
    private static readonly DateTime Now = new(2026, 8, 17, 12, 0, 0, DateTimeKind.Utc);

    private const string DeclaredInn = "7707083893";
    private const string OtherInn = "7736207543";

    private readonly ApplicationDbContext _db = InMemoryDbContextFactory.Create();
    private readonly Guid _startupId = Guid.NewGuid();

    private IPatentRegistryResolver CreateSut() => new PatentRegistryResolver(_db);

    [Fact]
    public async Task Resolve_ShouldReportRegistryUnavailable_PerKind()
    {
        // The program register is loaded; the trademark register is not. Merging those two states
        // would let a platform-side gap read as a statement about the startup.
        SeedStartup(inn: null);
        SeedRegistryEntry(IntellectualPropertyKind.ComputerProgram, "2023612345", DeclaredInn);
        SeedRecord(IntellectualPropertyKind.ComputerProgram, "2023619999");
        SeedRecord(IntellectualPropertyKind.Trademark, "812345");
        await _db.SaveChangesAsync();

        StartupPatentResolution resolution = await CreateSut().ResolveAsync(_startupId, default);

        resolution.Records
            .Single(r => r.Kind == IntellectualPropertyKind.ComputerProgram).State
            .ShouldBe(RegistryLookupState.NotFoundInRegistry);

        resolution.Records
            .Single(r => r.Kind == IntellectualPropertyKind.Trademark).State
            .ShouldBe(RegistryLookupState.RegistryUnavailable);
    }

    [Fact]
    public async Task Resolve_ShouldShowFoundRecordWithItsHolderAndStatus()
    {
        SeedStartup(inn: null);
        SeedRegistryEntry(
            IntellectualPropertyKind.ComputerProgram, "2023612345", DeclaredInn,
            title: "Программа учёта", holder: "ООО «Ромашка»",
            status: PatentProtectionStatus.EarlyTerminated);
        SeedRecord(IntellectualPropertyKind.ComputerProgram, "2023612345");
        await _db.SaveChangesAsync();

        ResolvedStartupPatent record = (await CreateSut().ResolveAsync(_startupId, default)).Records.Single();

        record.State.ShouldBe(RegistryLookupState.Found);
        record.Title.ShouldBe("Программа учёта");
        record.HolderName.ShouldBe("ООО «Ромашка»");
        record.ProtectionStatus.ShouldBe(PatentProtectionStatus.EarlyTerminated);

        // The startup declared no ИНН, so nothing about ownership is comparable — the holder is shown
        // as the register gives it, with no claim attached.
        record.Ownership.ShouldBe(DeclaredValueComparison.NotComparable);
    }

    [Theory]
    [InlineData(DeclaredInn, DeclaredInn, DeclaredValueComparison.Matches)]
    [InlineData(DeclaredInn, OtherInn, DeclaredValueComparison.Differs)]
    [InlineData(DeclaredInn, null, DeclaredValueComparison.NotComparable)]
    [InlineData(null, DeclaredInn, DeclaredValueComparison.NotComparable)]
    public async Task Resolve_ShouldCompareDeclaredInnWithTheHolder(
        string? declaredInn, string? holderInn, DeclaredValueComparison expected)
    {
        SeedStartup(declaredInn);
        SeedRegistryEntry(IntellectualPropertyKind.ComputerProgram, "2023612345", holderInn);
        SeedRecord(IntellectualPropertyKind.ComputerProgram, "2023612345");
        await _db.SaveChangesAsync();

        StartupPatentResolution resolution = await CreateSut().ResolveAsync(_startupId, default);

        resolution.DeclaredInn.ShouldBe(declaredInn);
        resolution.Records.Single().Ownership.ShouldBe(expected);
    }

    [Fact]
    public async Task HasRegistryCheckedOwnership_ShouldRequireBothTheRecordAndTheInn()
    {
        SeedStartup(DeclaredInn);
        SeedRegistryEntry(IntellectualPropertyKind.ComputerProgram, "2023612345", DeclaredInn);
        SeedRecord(IntellectualPropertyKind.ComputerProgram, "2023612345");
        await _db.SaveChangesAsync();

        IPatentRegistryResolver sut = CreateSut();

        (await sut.HasRegistryCheckedOwnershipAsync(_startupId, DeclaredInn, default)).ShouldBeTrue();

        // Without a declared ИНН there is nothing to compare against, and the register entry alone says
        // only who its own rightsholder is.
        (await sut.HasRegistryCheckedOwnershipAsync(_startupId, null, default)).ShouldBeFalse();
        (await sut.HasRegistryCheckedOwnershipAsync(_startupId, OtherInn, default)).ShouldBeFalse();
    }

    [Fact]
    public async Task HasRegistryCheckedOwnership_ShouldNotMatchAcrossKinds()
    {
        // Same digits, different register. Matching the two IN-lists independently would call this
        // checked; the pairs are re-matched in memory precisely so it is not.
        SeedStartup(DeclaredInn);
        SeedRegistryEntry(IntellectualPropertyKind.Database, "2023612345", DeclaredInn);
        SeedRegistryEntry(IntellectualPropertyKind.ComputerProgram, "2023619999", DeclaredInn);
        SeedRecord(IntellectualPropertyKind.ComputerProgram, "2023612345");
        await _db.SaveChangesAsync();

        (await CreateSut().HasRegistryCheckedOwnershipAsync(_startupId, DeclaredInn, default)).ShouldBeFalse();
    }

    [Fact]
    public async Task Resolve_ShouldReturnNothing_WhenTheStartupListedNoRecords()
    {
        SeedStartup(DeclaredInn);
        await _db.SaveChangesAsync();

        StartupPatentResolution resolution = await CreateSut().ResolveAsync(_startupId, default);

        resolution.Records.ShouldBeEmpty();
        resolution.DeclaredInn.ShouldBe(DeclaredInn);
    }

    private void SeedStartup(string? inn)
    {
        _db.Startups.Add(new Startup
        {
            Id = _startupId,
            Name = "Acme",
            PublicEmail = "acme@example.com",
            Stage = StartupStage.Mvp,
            HasPatents = true,
            Inn = inn,
            CreatedAt = Now,
            UpdatedAt = Now,
        });
    }

    private void SeedRecord(IntellectualPropertyKind kind, string number) =>
        _db.StartupPatents.Add(StartupPatent.Create(_startupId, kind, number, number, Now));

    private void SeedRegistryEntry(
        IntellectualPropertyKind kind,
        string number,
        string? holderInn,
        string? title = null,
        string? holder = null,
        PatentProtectionStatus status = PatentProtectionStatus.Active) =>
        _db.PatentRegistryEntries.Add(PatentRegistryEntry.Create(
            kind, number, title, holder, holderInn, new DateOnly(2023, 3, 15), status, "test", Now));
}
