using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Scoring;
using DevStart.Application.ScoringReports;
using DevStart.Application.ScoringReports.GetScoringReportDownloadUrl;
using DevStart.Application.Startups.GetScore;
using DevStart.Domain.ServiceOrders;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.Startups;
using DevStart.Domain.Subscriptions;
using DevStart.Infrastructure.Database;
using DevStart.Infrastructure.ScoringReports;
using DevStart.SharedKernel;
using DevStart.UnitTests.TestSupport;
using Shouldly;
using System.Security.Cryptography;

namespace DevStart.UnitTests.Application.ScoringReports;

/// <summary>
/// The report is the paid <see cref="ServiceType.ScoringReport"/> service finally delivering a
/// report. It must therefore open to exactly the same people the on-screen score opens to — no
/// wider, or the entitlement means less than it says; no narrower, or somebody who paid is refused.
/// </summary>
public sealed class GetScoringReportDownloadUrlQueryHandlerTests
{
    private static readonly Guid MemberId = Guid.Parse("dddddddd-1111-1111-1111-111111111111");
    private static readonly Guid OutsiderId = Guid.Parse("eeeeeeee-1111-1111-1111-111111111111");

    [Fact]
    public async Task AMemberOfTheStartup_GetsALinkWithoutPro()
    {
        Result<ScoringReportDownloadUrlResponse> result = await HandleAsync(MemberId, hasPro: false);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Url.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task AnOutsiderWithoutProOrEntitlement_IsRefused()
    {
        Result<ScoringReportDownloadUrlResponse> result = await HandleAsync(OutsiderId, hasPro: false);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(SubscriptionErrors.ProRequired);
    }

    [Fact]
    public async Task AnOutsiderWithPro_GetsALink()
    {
        Result<ScoringReportDownloadUrlResponse> result = await HandleAsync(OutsiderId, hasPro: true);

        result.IsSuccess.ShouldBeTrue();
    }

    /// <summary>The gap SC-67 closes: the paid "report" now hands over a report.</summary>
    [Fact]
    public async Task AnOutsiderWithAPaidScoringReport_GetsALink()
    {
        var entitlements = new StubServiceEntitlementChecker()
            .Grant(ServiceType.ScoringReport, ScoringReportSamples.StartupId);

        Result<ScoringReportDownloadUrlResponse> result =
            await HandleAsync(OutsiderId, hasPro: false, entitlements: entitlements);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task AnEntitlementForAnotherStartup_DoesNotOpenThisOne()
    {
        var entitlements = new StubServiceEntitlementChecker()
            .Grant(ServiceType.ScoringReport, Guid.NewGuid());

        Result<ScoringReportDownloadUrlResponse> result =
            await HandleAsync(OutsiderId, hasPro: false, entitlements: entitlements);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(SubscriptionErrors.ProRequired);
    }

    [Fact]
    public async Task AMissingStartup_IsNotFound()
    {
        Result<ScoringReportDownloadUrlResponse> result =
            await HandleAsync(MemberId, hasPro: true, startupId: Guid.NewGuid());

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(StartupErrors.NotFound(ScoringReportSamples.StartupId).Code);
    }

    /// <summary>
    /// The stored object is keyed by when the score was computed, so a later recomputation writes a
    /// new file instead of quietly replacing one somebody already holds.
    /// </summary>
    [Fact]
    public async Task TheFile_IsStoredUnderTheScoresComputationTime()
    {
        var storage = new CapturingFileStorage();

        Result<ScoringReportDownloadUrlResponse> result =
            await HandleAsync(MemberId, hasPro: false, storage: storage);

        string expectedKey = ScoringReportStorage.ObjectKey(
            ScoringReportSamples.StartupId, ScoringReportSamples.CalculatedAt);

        result.Value.CalculatedAt.ShouldBe(ScoringReportSamples.CalculatedAt);
        storage.Uploads.Single().ObjectKey.ShouldBe(expectedKey);
        storage.Uploads.Single().Bucket.ShouldBe(ScoringReportStorage.Bucket);
        storage.Uploads.Single().ContentType.ShouldBe("application/pdf");
        storage.Presigns.Single().ObjectKey.ShouldBe(expectedKey);
    }

    /// <summary>
    /// The key ends in Z, so it has to name an instant in UTC no matter what Kind the value arrives
    /// with. A Local value shifted by the machine's timezone would store one score under two keys on
    /// two machines — the silent kind of duplicate that only shows up as a storage bill.
    /// </summary>
    [Fact]
    public void TheObjectKey_NamesTheSameInstantWhateverTheDateTimeKind()
    {
        DateTime utc = new(2026, 5, 17, 9, 30, 0, DateTimeKind.Utc);
        string expected = ScoringReportStorage.ObjectKey(ScoringReportSamples.StartupId, utc);

        expected.ShouldEndWith("20260517T093000Z.pdf");

        // The same instant expressed as local time must land on the same key.
        ScoringReportStorage.ObjectKey(ScoringReportSamples.StartupId, utc.ToLocalTime())
            .ShouldBe(expected);

        // Unspecified is the platform's "already UTC": relabelled, never shifted.
        ScoringReportStorage.ObjectKey(
            ScoringReportSamples.StartupId, DateTime.SpecifyKind(utc, DateTimeKind.Unspecified))
            .ShouldBe(expected);
    }

    [Fact]
    public async Task TheLink_CarriesAMeaningfulFileNameAndTheContentHash()
    {
        var storage = new CapturingFileStorage();

        Result<ScoringReportDownloadUrlResponse> result =
            await HandleAsync(MemberId, hasPro: false, storage: storage);

        string expectedName = $"scoring-report-{ScoringReportSamples.StartupId}-2026-05-17.pdf";
        result.Value.FileName.ShouldBe(expectedName);
        storage.Presigns.Single().DownloadFileName.ShouldBe(expectedName);

        byte[] expectedPdf = new PdfScoringReportRenderer().Render(
            new ScoringReportComposer(new FixedDateTimeProvider { UtcNow = ScoringReportSamples.Now })
                .Compose(ScoringReportSamples.Startup(), ScoringReportSamples.Score()));
        result.Value.Sha256.ShouldBe(Convert.ToHexStringLower(SHA256.HashData(expectedPdf)));
    }

    /// <summary>
    /// A startup with nothing to score still gets a document — one that says so. Refusing would leave
    /// a paying reader with neither a file nor an explanation.
    /// </summary>
    [Fact]
    public async Task AStartupWithoutEnoughData_StillProducesAReport()
    {
        var storage = new CapturingFileStorage();

        Result<ScoringReportDownloadUrlResponse> result = await HandleAsync(
            MemberId, hasPro: false, storage: storage, score: ScoringReportSamples.NoData());

        result.IsSuccess.ShouldBeTrue();
        storage.Uploads.Count.ShouldBe(1);
    }

    [Fact]
    public async Task AStorageOutage_BecomesAServiceUnavailableResult()
    {
        var storage = new CapturingFileStorage
        {
            UploadException = new FileStorageException("down", notFound: false),
        };

        Result<ScoringReportDownloadUrlResponse> result =
            await HandleAsync(MemberId, hasPro: false, storage: storage);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("ScoringReports.StorageUnavailable");
    }

    private static async Task<Result<ScoringReportDownloadUrlResponse>> HandleAsync(
        Guid viewerId,
        bool hasPro,
        StubServiceEntitlementChecker? entitlements = null,
        CapturingFileStorage? storage = null,
        ScoreResult? score = null,
        Guid? startupId = null)
    {
        ApplicationDbContext context = InMemoryDbContextFactory.Create();
        context.Startups.Add(ScoringReportSamples.Startup());
        context.StartupMembers.Add(StartupMember.Create(
            MemberId, ScoringReportSamples.StartupId, StartupRole.Founder,
            isPublic: true, ScoringReportSamples.Now));
        await context.SaveChangesAsync();

        var clock = new FixedDateTimeProvider { UtcNow = ScoringReportSamples.Now };

        var handler = new GetScoringReportDownloadUrlQueryHandler(
            context,
            new StubScoreHandler(score ?? ScoringReportSamples.Score()),
            new TestUserContext(viewerId),
            new StubSubscriptionChecker(hasPro),
            entitlements ?? new StubServiceEntitlementChecker(),
            new ScoringReportComposer(clock),
            new PdfScoringReportRenderer(),
            storage ?? new CapturingFileStorage(),
            clock);

        return await handler.Handle(
            new GetScoringReportDownloadUrlQuery(startupId ?? ScoringReportSamples.StartupId),
            CancellationToken.None);
    }

    private sealed class StubScoreHandler(ScoreResult score) : IQueryHandler<ComputeStartupScoreQuery, ScoreResult>
    {
        public Task<Result<ScoreResult>> Handle(ComputeStartupScoreQuery query, CancellationToken cancellationToken)
            => Task.FromResult(Result.Success(score));
    }
}
