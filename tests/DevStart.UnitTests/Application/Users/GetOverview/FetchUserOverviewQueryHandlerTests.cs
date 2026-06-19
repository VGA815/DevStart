using DevStart.Application.Users.GetOverview;
using DevStart.Domain.ExpertCollaborationRequests;
using DevStart.Domain.Experts;
using DevStart.Domain.InvestmentApplications;
using DevStart.Domain.InvestmentDeals;
using DevStart.Domain.Investors;
using DevStart.Domain.Profiles;
using DevStart.Domain.Users;
using DevStart.Infrastructure.Database;
using DevStart.SharedKernel;
using DevStart.UnitTests.TestSupport;
using Shouldly;

namespace DevStart.UnitTests.Application.Users.GetOverview;

public sealed class FetchUserOverviewQueryHandlerTests
{
    private static readonly DateTime Now = new(2026, 5, 28, 10, 0, 0, DateTimeKind.Utc);
    private readonly ApplicationDbContext _db = InMemoryDbContextFactory.Create();

    [Fact]
    public async Task Returns_NotFound_WhenUserDoesNotExist()
    {
        var sut = new FetchUserOverviewQueryHandler(_db);

        Result<UserOverviewResponse> result =
            await sut.Handle(new FetchUserOverviewQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Users.NotFound");
    }

    [Fact]
    public async Task Returns_User_WithNullSections_WhenNoProfilesExist()
    {
        Guid userId = SeedUser();
        await _db.SaveChangesAsync();

        var sut = new FetchUserOverviewQueryHandler(_db);

        Result<UserOverviewResponse> result =
            await sut.Handle(new FetchUserOverviewQuery(userId), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Profile.ShouldBeNull();
        result.Value.InvestorProfile.ShouldBeNull();
        result.Value.ExpertProfile.ShouldBeNull();
        result.Value.Statistics.IsInvestor.ShouldBeFalse();
        result.Value.Statistics.IsExpert.ShouldBeFalse();
        result.Value.Statistics.CompletedDealsCount.ShouldBe(0);
        result.Value.Statistics.TotalInvestedAmount.ShouldBeNull();
    }

    [Fact]
    public async Task Aggregates_InvestorDealStats_CountingOnlyCompletedDeals()
    {
        Guid userId = SeedUser();
        SeedProfile(userId, "Acme Capital");
        SeedInvestorProfile(userId);
        SeedDeal(userId, 100_000m, InvestmentDealStatus.Completed);
        SeedDeal(userId, 50_000m, InvestmentDealStatus.Completed);
        SeedDeal(userId, 999_999m, InvestmentDealStatus.Cancelled);
        SeedDeal(userId, 12_345m, InvestmentDealStatus.InProgress);
        await _db.SaveChangesAsync();

        var sut = new FetchUserOverviewQueryHandler(_db);

        Result<UserOverviewResponse> result =
            await sut.Handle(new FetchUserOverviewQuery(userId), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.InvestorProfile.ShouldNotBeNull();
        result.Value.InvestorProfile!.DisplayName.ShouldBe("Acme Capital");
        result.Value.ExpertProfile.ShouldBeNull();
        result.Value.Statistics.IsInvestor.ShouldBeTrue();
        result.Value.Statistics.CompletedDealsCount.ShouldBe(2);
        result.Value.Statistics.TotalInvestedAmount.ShouldBe(150_000m);
    }

    [Fact]
    public async Task Aggregates_ExpertStats_CountingExperiencesAndAcceptedCollaborations()
    {
        Guid userId = SeedUser();
        SeedProfile(userId, "Jane Expert");
        SeedExpertProfile(userId);
        SeedExperience(userId);
        SeedExperience(userId);
        SeedCollaboration(userId, Guid.NewGuid(), accepted: true);
        SeedCollaboration(userId, Guid.NewGuid(), accepted: false);
        await _db.SaveChangesAsync();

        var sut = new FetchUserOverviewQueryHandler(_db);

        Result<UserOverviewResponse> result =
            await sut.Handle(new FetchUserOverviewQuery(userId), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ExpertProfile.ShouldNotBeNull();
        result.Value.InvestorProfile.ShouldBeNull();
        result.Value.Statistics.IsExpert.ShouldBeTrue();
        result.Value.Statistics.ExperiencesCount.ShouldBe(2);
        result.Value.Statistics.AcceptedCollaborationsCount.ShouldBe(1);
    }

    private Guid SeedUser(string username = "acme", string email = "acme@example.com")
    {
        User user = User.Create(username, email, "hash", Now);
        _db.Users.Add(user);
        return user.Id;
    }

    private void SeedProfile(Guid userId, string name)
    {
        _db.Profiles.Add(Profile.Create(userId, name, bio: null, url: null, isAvailableForHire: false, isPublic: true, avatarId: null));
    }

    private void SeedInvestorProfile(Guid userId)
    {
        _db.InvestorProfiles.Add(InvestorProfile.Create(userId, InvestorProfileType.Fund, Now));
    }

    private void SeedExpertProfile(Guid userId)
    {
        _db.ExpertProfiles.Add(ExpertProfile.Create(userId, Now));
    }

    private void SeedExperience(Guid expertProfileId)
    {
        _db.ExpertExperiences.Add(ExpertExperience.Create(
            expertProfileId,
            company: "Acme",
            position: "Advisor",
            startDate: new DateOnly(2020, 1, 1),
            endDate: null,
            description: null,
            createdAt: Now));
    }

    private void SeedCollaboration(Guid expertProfileId, Guid startupId, bool accepted)
    {
        ExpertCollaborationRequest request = ExpertCollaborationRequest.Create(
            expertProfileId,
            startupId,
            CollaborationType.Advisor,
            message: null,
            proposedHoursPerWeek: null,
            proposedRate: null,
            Now);

        if (accepted)
        {
            request.Accept(Now);
        }

        _db.ExpertCollaborationRequests.Add(request);
    }

    // InvestorProfile.Create sets Id == userId, so the investor profile id used by deals equals userId.
    private void SeedDeal(Guid investorProfileId, decimal amount, InvestmentDealStatus status)
    {
        _db.InvestmentDeals.Add(new InvestmentDeal
        {
            Id = Guid.NewGuid(),
            ApplicationId = Guid.NewGuid(),
            InvestorProfileId = investorProfileId,
            StartupId = Guid.NewGuid(),
            Amount = amount,
            Status = status,
            Instrument = InvestmentInstrument.Safe,
            CreatedAt = Now,
            UpdatedAt = Now,
            CompletedAt = status == InvestmentDealStatus.Completed ? Now : null
        });
    }
}
