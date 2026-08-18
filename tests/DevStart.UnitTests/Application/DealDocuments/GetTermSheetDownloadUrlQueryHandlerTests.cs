using DevStart.Application.DealDocuments.GetTermSheetDownloadUrl;
using DevStart.Domain.DealDocuments;
using DevStart.Domain.InvestmentDeals;
using DevStart.Domain.ServiceOrders;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.Subscriptions;
using DevStart.Infrastructure.Database;
using DevStart.SharedKernel;
using DevStart.UnitTests.TestSupport;
using Shouldly;

namespace DevStart.UnitTests.Application.DealDocuments;

/// <summary>
/// The PDF was added as a format on the existing query rather than as its own endpoint, so that the
/// access rule stays in one place. These tests hold that promise to account: every gate is asserted
/// for the PDF as well as for the markdown, and a divergence between them fails here.
/// </summary>
public sealed class GetTermSheetDownloadUrlQueryHandlerTests
{
    private static readonly DateTime Now = new(2026, 5, 17, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime GeneratedAt = new(2026, 5, 16, 9, 0, 0, DateTimeKind.Utc);
    private static readonly Guid DealId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid StartupId = Guid.Parse("cccccccc-1111-1111-1111-111111111111");
    private static readonly Guid InvestorId = Guid.Parse("bbbbbbbb-1111-1111-1111-111111111111");
    private static readonly Guid FounderId = Guid.Parse("dddddddd-1111-1111-1111-111111111111");
    private static readonly Guid OutsiderId = Guid.Parse("eeeeeeee-1111-1111-1111-111111111111");
    private const string PdfHash = "9f2c1b7a4d5e6f708192a3b4c5d6e7f8091a2b3c4d5e6f708192a3b4c5d6e7f8";

    [Theory]
    [InlineData(TermSheetFormat.Markdown)]
    [InlineData(TermSheetFormat.Pdf)]
    public async Task AFounder_GetsALinkWithoutPro(TermSheetFormat format)
    {
        Result<TermSheetDownloadUrlResponse> result = await HandleAsync(FounderId, format, hasPro: false);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Url.ShouldNotBeNullOrEmpty();
    }

    [Theory]
    [InlineData(TermSheetFormat.Markdown)]
    [InlineData(TermSheetFormat.Pdf)]
    public async Task AnInvestorWithoutProOrEntitlement_IsRefused(TermSheetFormat format)
    {
        Result<TermSheetDownloadUrlResponse> result = await HandleAsync(InvestorId, format, hasPro: false);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(SubscriptionErrors.ProRequired);
    }

    [Theory]
    [InlineData(TermSheetFormat.Markdown)]
    [InlineData(TermSheetFormat.Pdf)]
    public async Task AnInvestorWithPro_GetsALink(TermSheetFormat format)
    {
        Result<TermSheetDownloadUrlResponse> result = await HandleAsync(InvestorId, format, hasPro: true);

        result.IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData(TermSheetFormat.Markdown)]
    [InlineData(TermSheetFormat.Pdf)]
    public async Task AnInvestorWithAPaidTermSheet_GetsALink(TermSheetFormat format)
    {
        var entitlements = new StubServiceEntitlementChecker().Grant(ServiceType.TermSheet, DealId);

        Result<TermSheetDownloadUrlResponse> result =
            await HandleAsync(InvestorId, format, hasPro: false, entitlements: entitlements);

        result.IsSuccess.ShouldBeTrue();
    }

    /// <summary>An entitlement is per deal — paying for one term sheet does not open another.</summary>
    [Theory]
    [InlineData(TermSheetFormat.Markdown)]
    [InlineData(TermSheetFormat.Pdf)]
    public async Task AnEntitlementForAnotherDeal_DoesNotOpenThisOne(TermSheetFormat format)
    {
        var entitlements = new StubServiceEntitlementChecker().Grant(ServiceType.TermSheet, Guid.NewGuid());

        Result<TermSheetDownloadUrlResponse> result =
            await HandleAsync(InvestorId, format, hasPro: false, entitlements: entitlements);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(SubscriptionErrors.ProRequired);
    }

    [Theory]
    [InlineData(TermSheetFormat.Markdown)]
    [InlineData(TermSheetFormat.Pdf)]
    public async Task SomeoneUnrelatedToTheDeal_IsRefused(TermSheetFormat format)
    {
        Result<TermSheetDownloadUrlResponse> result = await HandleAsync(OutsiderId, format, hasPro: true);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(DealDocumentErrors.Unauthorized);
    }

    [Fact]
    public async Task ThePdfFormat_LinksToThePdfAndCarriesItsHash()
    {
        var storage = new CapturingFileStorage();

        Result<TermSheetDownloadUrlResponse> result =
            await HandleAsync(FounderId, TermSheetFormat.Pdf, hasPro: false, storage: storage);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Format.ShouldBe(TermSheetFormat.Pdf);
        result.Value.Sha256.ShouldBe(PdfHash);
        result.Value.FileName.ShouldBe($"term-sheet-{DealId}-2026-05-16.pdf");

        CapturingFileStorage.PresignCall call = storage.Presigns.Single();
        call.ObjectKey.ShouldBe($"deal-documents/{DealId}/term-sheet.pdf");
        call.DownloadFileName.ShouldBe(result.Value.FileName);
    }

    [Fact]
    public async Task TheMarkdownFormat_LinksToTheMarkdownAndHasNoHash()
    {
        var storage = new CapturingFileStorage();

        Result<TermSheetDownloadUrlResponse> result =
            await HandleAsync(FounderId, TermSheetFormat.Markdown, hasPro: false, storage: storage);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Sha256.ShouldBeNull();
        result.Value.FileName.ShouldBe($"term-sheet-{DealId}-2026-05-16.md");
        storage.Presigns.Single().ObjectKey.ShouldBe($"deal-documents/{DealId}/term-sheet.md");
    }

    /// <summary>
    /// A document set from before PDF rendering existed still serves its markdown. Asking for the PDF
    /// gets a specific "not generated yet" answer rather than a link to an object that is not there.
    /// </summary>
    [Fact]
    public async Task ADocumentSetWithoutAPdf_RefusesThePdfButStillServesTheMarkdown()
    {
        Result<TermSheetDownloadUrlResponse> pdf =
            await HandleAsync(FounderId, TermSheetFormat.Pdf, hasPro: false, withPdf: false);
        pdf.IsFailure.ShouldBeTrue();
        pdf.Error.Code.ShouldBe("DealDocuments.PdfNotGenerated");

        Result<TermSheetDownloadUrlResponse> markdown =
            await HandleAsync(FounderId, TermSheetFormat.Markdown, hasPro: false, withPdf: false);
        markdown.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task AMissingDeal_IsNotFound()
    {
        Result<TermSheetDownloadUrlResponse> result =
            await HandleAsync(FounderId, TermSheetFormat.Pdf, hasPro: true, dealId: Guid.NewGuid());

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(InvestmentDealErrors.NotFound(DealId).Code);
    }

    private static async Task<Result<TermSheetDownloadUrlResponse>> HandleAsync(
        Guid viewerId,
        TermSheetFormat format,
        bool hasPro,
        StubServiceEntitlementChecker? entitlements = null,
        CapturingFileStorage? storage = null,
        bool withPdf = true,
        Guid? dealId = null)
    {
        ApplicationDbContext context = InMemoryDbContextFactory.Create();

        context.InvestmentDeals.Add(new InvestmentDeal
        {
            Id = DealId,
            ApplicationId = Guid.NewGuid(),
            InvestorProfileId = InvestorId,
            StartupId = StartupId,
            Amount = 1_000_000m,
        });

        context.StartupMembers.Add(
            StartupMember.Create(FounderId, StartupId, StartupRole.Founder, isPublic: true, Now));

        context.DealDocuments.Add(new DealDocument
        {
            Id = Guid.NewGuid(),
            DealId = DealId,
            TermSheetObjectKey = $"deal-documents/{DealId}/term-sheet.md",
            TermSheetPdfObjectKey = withPdf ? $"deal-documents/{DealId}/term-sheet.pdf" : string.Empty,
            TermSheetPdfSha256 = withPdf ? PdfHash : string.Empty,
            CapTableObjectKey = $"deal-documents/{DealId}/cap-table.json",
            GeneratedAt = GeneratedAt,
        });

        await context.SaveChangesAsync();

        var handler = new GetTermSheetDownloadUrlQueryHandler(
            context,
            new TestUserContext(viewerId),
            storage ?? new CapturingFileStorage(),
            new FixedDateTimeProvider { UtcNow = Now },
            new StubSubscriptionChecker(hasPro),
            entitlements ?? new StubServiceEntitlementChecker());

        return await handler.Handle(
            new GetTermSheetDownloadUrlQuery(dealId ?? DealId, format), CancellationToken.None);
    }
}
