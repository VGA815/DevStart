using DevStart.Domain.InvestmentApplications;
using Shouldly;

namespace DevStart.UnitTests.Domain.InvestmentApplications;

public sealed class InvestmentApplicationTests
{
    private static readonly DateTime CreatedAt = new(2026, 5, 16, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime ChangedAt = new(2026, 5, 16, 11, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_ShouldInitializeApplicationWithDefaultsAndDealTerms()
    {
        Guid investorProfileId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        Guid startupId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        Guid roadmapItemId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

        InvestmentApplication application = InvestmentApplication.Create(
            investorProfileId,
            startupId,
            roadmapItemId,
            5_000_000m,
            "Interested",
            CreatedAt,
            InvestmentInstrument.ConvertibleLoan,
            valuationCap: 50_000_000m,
            discount: 0.2m,
            interestRate: 0.06m,
            termMonths: 24,
            preMoneyValuation: null,
            liquidationPreference: 1.5m,
            proRataRights: true);

        application.Id.ShouldNotBe(Guid.Empty);
        application.InvestorProfileId.ShouldBe(investorProfileId);
        application.StartupId.ShouldBe(startupId);
        application.RoadmapItemId.ShouldBe(roadmapItemId);
        application.Amount.ShouldBe(5_000_000m);
        application.Message.ShouldBe("Interested");
        application.Status.ShouldBe(InvestmentApplicationStatus.Pending);
        application.Instrument.ShouldBe(InvestmentInstrument.ConvertibleLoan);
        application.ValuationCap.ShouldBe(50_000_000m);
        application.Discount.ShouldBe(0.2m);
        application.InterestRate.ShouldBe(0.06m);
        application.TermMonths.ShouldBe(24);
        application.LiquidationPreference.ShouldBe(1.5m);
        application.ProRataRights.ShouldBeTrue();
        application.CreatedAt.ShouldBe(CreatedAt);
        application.UpdatedAt.ShouldBe(CreatedAt);
    }

    [Fact]
    public void Accept_ShouldMovePendingApplicationToAccepted()
    {
        InvestmentApplication application = CreatePendingApplication();

        var result = application.Accept(ChangedAt);

        result.IsSuccess.ShouldBeTrue();
        application.Status.ShouldBe(InvestmentApplicationStatus.Accepted);
        application.UpdatedAt.ShouldBe(ChangedAt);
    }

    [Fact]
    public void Reject_ShouldMovePendingApplicationToRejected()
    {
        InvestmentApplication application = CreatePendingApplication();

        var result = application.Reject(ChangedAt);

        result.IsSuccess.ShouldBeTrue();
        application.Status.ShouldBe(InvestmentApplicationStatus.Rejected);
        application.UpdatedAt.ShouldBe(ChangedAt);
    }

    [Fact]
    public void Withdraw_ShouldMovePendingApplicationToWithdrawn()
    {
        InvestmentApplication application = CreatePendingApplication();

        var result = application.Withdraw(ChangedAt);

        result.IsSuccess.ShouldBeTrue();
        application.Status.ShouldBe(InvestmentApplicationStatus.Withdrawn);
        application.UpdatedAt.ShouldBe(ChangedAt);
    }

    [Fact]
    public void StatusTransitions_ShouldFail_WhenApplicationIsNotPending()
    {
        InvestmentApplication application = CreatePendingApplication();
        application.Accept(ChangedAt);

        var rejectResult = application.Reject(ChangedAt.AddMinutes(1));
        var withdrawResult = application.Withdraw(ChangedAt.AddMinutes(1));

        rejectResult.IsFailure.ShouldBeTrue();
        rejectResult.Error.ShouldBe(InvestmentApplicationErrors.MustBePending);
        withdrawResult.IsFailure.ShouldBeTrue();
        withdrawResult.Error.ShouldBe(InvestmentApplicationErrors.MustBePending);
        application.Status.ShouldBe(InvestmentApplicationStatus.Accepted);
        application.UpdatedAt.ShouldBe(ChangedAt);
    }

    private static InvestmentApplication CreatePendingApplication() =>
        InvestmentApplication.Create(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            roadmapItemId: null,
            1_000_000m,
            message: null,
            CreatedAt);
}
