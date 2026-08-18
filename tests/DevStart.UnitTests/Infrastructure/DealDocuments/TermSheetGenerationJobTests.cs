using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.DealDocuments.Generation;
using DevStart.Application.Scoring;
using DevStart.Application.StartupEquity;
using DevStart.Application.StartupEquity.Vesting;
using DevStart.Application.Startups.GetScore;
using DevStart.Domain.DealDocuments;
using DevStart.Domain.InvestmentDeals;
using DevStart.Domain.Notifications;
using DevStart.Domain.Startups;
using DevStart.Infrastructure.Database;
using DevStart.Infrastructure.DealDocuments;
using DevStart.Infrastructure.DealDocuments.Generation;
using DevStart.SharedKernel;
using DevStart.UnitTests.Application.DealDocuments;
using DevStart.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using System.Security.Cryptography;

namespace DevStart.UnitTests.Infrastructure.DealDocuments;

public sealed class TermSheetGenerationJobTests
{
    private static readonly DateTime Now = new(2026, 5, 17, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Generating_StoresAllThreeObjectsAndTheHash()
    {
        Harness harness = await Harness.CreateAsync();

        await harness.Job.GenerateAsync(harness.DealId, CancellationToken.None);

        DealDocument document = await harness.Context.DealDocuments.SingleAsync();
        document.TermSheetObjectKey.ShouldBe(DealDocumentBuckets.TermSheetObjectKey(harness.DealId));
        document.TermSheetPdfObjectKey.ShouldBe(DealDocumentBuckets.TermSheetPdfObjectKey(harness.DealId));
        document.CapTableObjectKey.ShouldBe(DealDocumentBuckets.CapTableObjectKey(harness.DealId));
        document.TermSheetPdfSha256.Length.ShouldBe(64);

        harness.Storage.Uploaded.Keys.ShouldContain(document.TermSheetObjectKey);
        harness.Storage.Uploaded.Keys.ShouldContain(document.TermSheetPdfObjectKey);
        harness.Storage.Uploaded.Keys.ShouldContain(document.CapTableObjectKey);
    }

    /// <summary>The hash must describe the bytes that were actually stored, not something adjacent to them.</summary>
    [Fact]
    public async Task TheStoredHash_IsTheHashOfTheStoredPdf()
    {
        Harness harness = await Harness.CreateAsync();

        await harness.Job.GenerateAsync(harness.DealId, CancellationToken.None);

        DealDocument document = await harness.Context.DealDocuments.SingleAsync();
        byte[] stored = harness.Storage.Uploaded[document.TermSheetPdfObjectKey];

        document.TermSheetPdfSha256.ShouldBe(Convert.ToHexStringLower(SHA256.HashData(stored)));
    }

    /// <summary>
    /// Deterministic rendering plus a hash over the final bytes means a second run on the same inputs
    /// reproduces the same fingerprint. Without that the hash records when the job ran, not what it
    /// produced.
    /// </summary>
    [Fact]
    public async Task RunningTwiceOnTheSameInputs_ReproducesTheSameHash()
    {
        Harness first = await Harness.CreateAsync();
        await first.Job.GenerateAsync(first.DealId, CancellationToken.None);

        Harness second = await Harness.CreateAsync();
        await second.Job.GenerateAsync(second.DealId, CancellationToken.None);

        (await second.Context.DealDocuments.SingleAsync()).TermSheetPdfSha256
            .ShouldBe((await first.Context.DealDocuments.SingleAsync()).TermSheetPdfSha256);
    }

    /// <summary>
    /// The whole point of rendering before uploading: a failed render must not leave a half-written
    /// document set behind for Hangfire's retry — or for a reader — to trip over.
    /// </summary>
    [Fact]
    public async Task AFailingPdfRender_UploadsNothingAndWritesNoRow()
    {
        Harness harness = await Harness.CreateAsync(pdfRenderer: new ThrowingPdfRenderer());

        await Should.ThrowAsync<InvalidOperationException>(
            () => harness.Job.GenerateAsync(harness.DealId, CancellationToken.None));

        harness.Storage.Uploaded.ShouldBeEmpty();
        (await harness.Context.DealDocuments.AnyAsync()).ShouldBeFalse();
        harness.Notifications.Published.ShouldBeEmpty();
    }

    [Fact]
    public async Task ACompleteDocumentSet_IsNeverRegenerated()
    {
        Harness harness = await Harness.CreateAsync();
        await harness.Job.GenerateAsync(harness.DealId, CancellationToken.None);

        DealDocument before = await harness.Context.DealDocuments.AsNoTracking().SingleAsync();
        harness.Storage.Uploaded.Clear();
        harness.Notifications.Published.Clear();

        await harness.Job.GenerateAsync(harness.DealId, CancellationToken.None);

        harness.Storage.Uploaded.ShouldBeEmpty();
        harness.Notifications.Published.ShouldBeEmpty();
        DealDocument after = await harness.Context.DealDocuments.AsNoTracking().SingleAsync();
        after.Id.ShouldBe(before.Id);
        after.GeneratedAt.ShouldBe(before.GeneratedAt);
        (await harness.Context.DealDocuments.CountAsync()).ShouldBe(1);
    }

    /// <summary>
    /// A row written before PDF generation existed has an empty PDF key. Since the job stops at the
    /// sight of any row, that row would keep the gap forever unless it is treated as incomplete.
    /// </summary>
    [Fact]
    public async Task ARowWrittenBeforePdfsExisted_IsCompletedInPlace()
    {
        Harness harness = await Harness.CreateAsync();
        harness.Context.DealDocuments.Add(new DealDocument
        {
            Id = Guid.NewGuid(),
            DealId = harness.DealId,
            TermSheetObjectKey = DealDocumentBuckets.TermSheetObjectKey(harness.DealId),
            TermSheetPdfObjectKey = string.Empty,
            TermSheetPdfSha256 = string.Empty,
            CapTableObjectKey = DealDocumentBuckets.CapTableObjectKey(harness.DealId),
            GeneratedAt = Now.AddDays(-5),
        });
        await harness.Context.SaveChangesAsync();
        Guid originalId = (await harness.Context.DealDocuments.AsNoTracking().SingleAsync()).Id;

        await harness.Job.GenerateAsync(harness.DealId, CancellationToken.None);

        DealDocument document = await harness.Context.DealDocuments.AsNoTracking().SingleAsync();
        document.Id.ShouldBe(originalId);
        document.TermSheetPdfObjectKey.ShouldBe(DealDocumentBuckets.TermSheetPdfObjectKey(harness.DealId));
        document.TermSheetPdfSha256.Length.ShouldBe(64);
        (await harness.Context.DealDocuments.CountAsync()).ShouldBe(1);

        // The recipients were told about this document set when it was first produced; a file being
        // added to it is not a second announcement.
        harness.Notifications.Published.ShouldBeEmpty();
    }

    [Fact]
    public async Task ANewDocumentSet_NotifiesTheInvestor()
    {
        Harness harness = await Harness.CreateAsync();

        await harness.Job.GenerateAsync(harness.DealId, CancellationToken.None);

        harness.Notifications.Published.ShouldContain(n =>
            n.Type == NotificationType.DealDocumentsReady && n.UserId == harness.InvestorId);
    }

    private sealed class Harness
    {
        public required ApplicationDbContext Context { get; init; }
        public required TermSheetGenerationJob Job { get; init; }
        public required RecordingUploadStorage Storage { get; init; }
        public required RecordingNotificationService Notifications { get; init; }
        public required Guid DealId { get; init; }
        public required Guid InvestorId { get; init; }

        public static async Task<Harness> CreateAsync(ITermSheetPdfRenderer? pdfRenderer = null)
        {
            TermSheetSamples.Sample sample = TermSheetSamples.Safe();
            ApplicationDbContext context = InMemoryDbContextFactory.Create();
            context.Startups.Add(sample.Startup);
            context.InvestmentDeals.Add(sample.Deal);
            await context.SaveChangesAsync();

            var storage = new RecordingUploadStorage();
            var notifications = new RecordingNotificationService();
            var clock = new FixedDateTimeProvider { UtcNow = Now };

            var job = new TermSheetGenerationJob(
                context,
                new CapTableCalculator(),
                new TermSheetComposer(new VestingCalculator(), clock),
                new MarkdownTermSheetRenderer(storage),
                pdfRenderer ?? new PdfTermSheetRenderer(),
                new StubCapTableProvider(sample.FoundingHolders),
                new VestingCalculator(),
                storage,
                notifications,
                clock,
                new StubScoreHandler(sample.Score),
                NullLogger<TermSheetGenerationJob>.Instance);

            return new Harness
            {
                Context = context,
                Job = job,
                Storage = storage,
                Notifications = notifications,
                DealId = sample.Deal.Id,
                InvestorId = sample.Deal.InvestorProfileId,
            };
        }
    }

    /// <summary>Serves the real templates for the markdown render and keeps every uploaded payload.</summary>
    private sealed class RecordingUploadStorage : IFileStorage
    {
        private readonly TemplateFileStorage _templates = new();

        public Dictionary<string, byte[]> Uploaded { get; } = [];

        public async Task UploadAsync(string objectKey, Stream data, string bucket, string contentType, CancellationToken cancellationToken)
        {
            using var buffer = new MemoryStream();
            await data.CopyToAsync(buffer, cancellationToken);
            Uploaded[objectKey] = buffer.ToArray();
        }

        public Task<Stream> DownloadAsync(string objectName, string bucket, CancellationToken cancellationToken)
            => _templates.DownloadAsync(objectName, bucket, cancellationToken);

        public Task DeleteAsync(string objectKey, string bucket, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task<string> GetPresignedUrl(string objectKey, string bucket, int expirySeconds, CancellationToken cancellationToken, string? downloadFileName = null)
            => Task.FromResult($"https://example.com/{bucket}/{objectKey}");
    }

    private sealed class ThrowingPdfRenderer : ITermSheetPdfRenderer
    {
        public byte[] Render(TermSheetModel model) => throw new InvalidOperationException("layout failed");
    }

    private sealed class StubCapTableProvider(IReadOnlyList<FoundingCapTableHolder> holders) : IFoundingCapTableProvider
    {
        public Task<IReadOnlyList<FoundingCapTableHolder>> GetEffectiveHoldersAsync(Guid startupId, CancellationToken cancellationToken)
            => Task.FromResult(holders);
    }

    private sealed class StubScoreHandler(ScoreResult score) : IQueryHandler<ComputeStartupScoreQuery, ScoreResult>
    {
        public Task<Result<ScoreResult>> Handle(ComputeStartupScoreQuery query, CancellationToken cancellationToken)
            => Task.FromResult(Result.Success(score));
    }
}
