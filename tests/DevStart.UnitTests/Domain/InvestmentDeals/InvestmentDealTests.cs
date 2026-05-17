using DevStart.Domain.InvestmentApplications;
using DevStart.Domain.InvestmentDeals;
using Shouldly;

namespace DevStart.UnitTests.Domain.InvestmentDeals;

public sealed class InvestmentDealTests
{
    private static readonly DateTime CreatedAt = new(2026, 5, 16, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime ChangedAt = new(2026, 5, 16, 11, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void CreateFromApplication_ShouldCopyApplicationFieldsAndInitializeDeal()
    {
        InvestmentApplication application = CreateApplication();

        InvestmentDeal deal = InvestmentDeal.CreateFromApplication(application, CreatedAt);

        deal.Id.ShouldNotBe(Guid.Empty);
        deal.ApplicationId.ShouldBe(application.Id);
        deal.InvestorProfileId.ShouldBe(application.InvestorProfileId);
        deal.StartupId.ShouldBe(application.StartupId);
        deal.RoadmapItemId.ShouldBe(application.RoadmapItemId);
        deal.Amount.ShouldBe(application.Amount);
        deal.Instrument.ShouldBe(application.Instrument);
        deal.ValuationCap.ShouldBe(application.ValuationCap);
        deal.Discount.ShouldBe(application.Discount);
        deal.InterestRate.ShouldBe(application.InterestRate);
        deal.TermMonths.ShouldBe(application.TermMonths);
        deal.PreMoneyValuation.ShouldBe(application.PreMoneyValuation);
        deal.LiquidationPreference.ShouldBe(application.LiquidationPreference);
        deal.ProRataRights.ShouldBe(application.ProRataRights);
        deal.Status.ShouldBe(InvestmentDealStatus.InProgress);
        deal.ConfirmedByStartup.ShouldBeFalse();
        deal.ConfirmedByInvestor.ShouldBeFalse();
        deal.CompletedAt.ShouldBeNull();
        deal.CreatedAt.ShouldBe(CreatedAt);
        deal.UpdatedAt.ShouldBe(CreatedAt);
    }

    [Fact]
    public void ConfirmByStartup_ShouldKeepDealInProgress_WhenOnlyStartupConfirmed()
    {
        InvestmentDeal deal = InvestmentDeal.CreateFromApplication(CreateApplication(), CreatedAt);

        var result = deal.ConfirmByStartup(ChangedAt);

        result.IsSuccess.ShouldBeTrue();
        deal.ConfirmedByStartup.ShouldBeTrue();
        deal.ConfirmedByInvestor.ShouldBeFalse();
        deal.Status.ShouldBe(InvestmentDealStatus.InProgress);
        deal.CompletedAt.ShouldBeNull();
        deal.DomainEvents.ShouldBeEmpty();
        deal.UpdatedAt.ShouldBe(ChangedAt);
    }

    [Fact]
    public void ConfirmByInvestor_ShouldCompleteDealAndRaiseDomainEvent_WhenStartupAlreadyConfirmed()
    {
        InvestmentDeal deal = InvestmentDeal.CreateFromApplication(CreateApplication(), CreatedAt);
        deal.ConfirmByStartup(ChangedAt);
        DateTime completedAt = ChangedAt.AddMinutes(10);

        var result = deal.ConfirmByInvestor(completedAt);

        result.IsSuccess.ShouldBeTrue();
        deal.ConfirmedByInvestor.ShouldBeTrue();
        deal.Status.ShouldBe(InvestmentDealStatus.Completed);
        deal.CompletedAt.ShouldBe(completedAt);
        deal.UpdatedAt.ShouldBe(completedAt);
        InvestmentDealCompletedDomainEvent domainEvent = deal.DomainEvents
            .ShouldHaveSingleItem()
            .ShouldBeOfType<InvestmentDealCompletedDomainEvent>();
        domainEvent.DealId.ShouldBe(deal.Id);
        domainEvent.ApplicationId.ShouldBe(deal.ApplicationId);
        domainEvent.InvestorProfileId.ShouldBe(deal.InvestorProfileId);
        domainEvent.StartupId.ShouldBe(deal.StartupId);
    }

    [Fact]
    public void ConfirmByStartup_ShouldFail_WhenAlreadyConfirmed()
    {
        InvestmentDeal deal = InvestmentDeal.CreateFromApplication(CreateApplication(), CreatedAt);
        deal.ConfirmByStartup(ChangedAt);

        var result = deal.ConfirmByStartup(ChangedAt.AddMinutes(1));

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(InvestmentDealErrors.AlreadyConfirmed);
        deal.UpdatedAt.ShouldBe(ChangedAt);
    }

    [Fact]
    public void Confirmation_ShouldFail_WhenDealAlreadyCompleted()
    {
        InvestmentDeal deal = InvestmentDeal.CreateFromApplication(CreateApplication(), CreatedAt);
        deal.ConfirmByStartup(ChangedAt);
        deal.ConfirmByInvestor(ChangedAt.AddMinutes(1));

        var result = deal.ConfirmByStartup(ChangedAt.AddMinutes(2));

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(InvestmentDealErrors.AlreadyCompleted);
    }

    private static InvestmentApplication CreateApplication() =>
        InvestmentApplication.Create(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            5_000_000m,
            "Interested",
            CreatedAt,
            InvestmentInstrument.ConvertibleLoan,
            valuationCap: 50_000_000m,
            discount: 0.2m,
            interestRate: 0.06m,
            termMonths: 24,
            liquidationPreference: 1.5m,
            proRataRights: true);
}
