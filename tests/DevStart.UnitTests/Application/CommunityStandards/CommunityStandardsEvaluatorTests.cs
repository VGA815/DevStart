using DevStart.Application;
using DevStart.Application.CommunityStandards;
using DevStart.Domain.StartupCommunityStandards;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace DevStart.UnitTests.Application.CommunityStandards;

public sealed class CommunityStandardsEvaluatorTests
{
    private static readonly DateTime EvaluatedAt = new(2026, 7, 23, 10, 0, 0, DateTimeKind.Utc);

    private static readonly CommunityDocumentType[] AllDocumentTypes =
        Enum.GetValues<CommunityDocumentType>();

    private readonly ICommunityStandardsEvaluator _evaluator = new ServiceCollection()
        .AddApplication()
        .BuildServiceProvider()
        .GetRequiredService<ICommunityStandardsEvaluator>();

    [Fact]
    public void Evaluate_ShouldReportNothingSatisfied_WhenStartupIsEmpty()
    {
        CommunityStandardsResult result = _evaluator.Evaluate(Inputs(), EvaluatedAt);

        result.TotalCount.ShouldBe(12);
        result.CompletedCount.ShouldBe(0);
        result.Percent.ShouldBe(0m);
        result.Level.ShouldBe(CommunityStandardsLevel.Incomplete);
        result.Checks.ShouldAllBe(c => !c.IsSatisfied);
        result.EvaluatedAt.ShouldBe(EvaluatedAt);
    }

    [Fact]
    public void Evaluate_ShouldReportComplete_WhenEveryCheckPasses()
    {
        CommunityStandardsResult result = _evaluator.Evaluate(FullyCompliantInputs(), EvaluatedAt);

        result.CompletedCount.ShouldBe(12);
        result.Percent.ShouldBe(100m);
        result.Level.ShouldBe(CommunityStandardsLevel.Complete);
        result.Checks.ShouldAllBe(c => c.IsSatisfied);
    }

    [Fact]
    public void Evaluate_ShouldExposeSevenProfileChecksAndFiveDocumentChecks()
    {
        CommunityStandardsResult result = _evaluator.Evaluate(Inputs(), EvaluatedAt);

        result.Checks.Count(c => !c.IsDocument).ShouldBe(7);
        result.Checks.Count(c => c.IsDocument).ShouldBe(5);

        // Every document type gets a row, so the client can render an "Add" action for each.
        result.Checks
            .Where(c => c.IsDocument)
            .Select(c => c.DocumentType)
            .ShouldBe(AllDocumentTypes.Cast<CommunityDocumentType?>(), ignoreOrder: true);
    }

    [Fact]
    public void Evaluate_ShouldSurfaceDocumentId_WhenDocumentExists()
    {
        Guid documentId = Guid.NewGuid();

        CommunityStandardsResult result = _evaluator.Evaluate(
            Inputs(documents: new Dictionary<CommunityDocumentType, Guid>
            {
                [CommunityDocumentType.CodeOfConduct] = documentId
            }),
            EvaluatedAt);

        CommunityStandardsCheck codeOfConduct = result.Checks.Single(c => c.Key == "code_of_conduct");
        codeOfConduct.IsSatisfied.ShouldBeTrue();
        codeOfConduct.DocumentId.ShouldBe(documentId);

        // The rows for documents that were not published carry no id to link to.
        result.Checks
            .Where(c => c.IsDocument && c.Key != "code_of_conduct")
            .ShouldAllBe(c => c.DocumentId == null);
    }

    [Theory]
    [InlineData(5, CommunityStandardsLevel.Incomplete)]
    [InlineData(6, CommunityStandardsLevel.Developing)]
    [InlineData(11, CommunityStandardsLevel.Developing)]
    [InlineData(12, CommunityStandardsLevel.Complete)]
    public void Evaluate_ShouldGradeOnCompletedCount(int satisfiedCount, CommunityStandardsLevel expected)
    {
        CommunityStandardsResult result = _evaluator.Evaluate(InputsWithFirst(satisfiedCount), EvaluatedAt);

        result.CompletedCount.ShouldBe(satisfiedCount);
        result.Level.ShouldBe(expected);
    }

    [Fact]
    public void Evaluate_ShouldRequireBothDescriptionsProductFieldsAndAFounderWithColleague()
    {
        // A lone founder is not a team, so the team check stays unsatisfied.
        CommunityStandardsResult soloFounder = _evaluator.Evaluate(
            Inputs(memberCount: 1, hasFounder: true), EvaluatedAt);
        soloFounder.Checks.Single(c => c.Key == "team").IsSatisfied.ShouldBeFalse();

        // Two members without a founder is likewise not enough.
        CommunityStandardsResult founderless = _evaluator.Evaluate(
            Inputs(memberCount: 2, hasFounder: false), EvaluatedAt);
        founderless.Checks.Single(c => c.Key == "team").IsSatisfied.ShouldBeFalse();

        CommunityStandardsResult team = _evaluator.Evaluate(
            Inputs(memberCount: 2, hasFounder: true), EvaluatedAt);
        team.Checks.Single(c => c.Key == "team").IsSatisfied.ShouldBeTrue();
    }

    [Fact]
    public void Evaluate_ShouldAcceptTwoCoFoundersAsATeam()
    {
        // Deliberate: a two-co-founder startup that has not hired yet still has a team. The check counts
        // people, not employment status, so it must not require a non-founder member.
        CommunityStandardsResult result = _evaluator.Evaluate(
            Inputs(memberCount: 2, hasFounder: true), EvaluatedAt);

        result.Checks.Single(c => c.Key == "team").IsSatisfied.ShouldBeTrue();
    }

    [Theory]
    [InlineData(2, false)]
    [InlineData(3, true)]
    public void Evaluate_ShouldRequireThreeRoadmapItems(int itemCount, bool expected)
    {
        CommunityStandardsResult result = _evaluator.Evaluate(Inputs(roadmapItemCount: itemCount), EvaluatedAt);

        result.Checks.Single(c => c.Key == "roadmap").IsSatisfied.ShouldBe(expected);
    }

    [Fact]
    public void Evaluate_ShouldRoundPercentToWholeNumbers()
    {
        // 7 of 12 is 58.33…% — the badge shows whole percents.
        CommunityStandardsResult result = _evaluator.Evaluate(InputsWithFirst(7), EvaluatedAt);

        result.Percent.ShouldBe(58m);
    }

    private static CommunityStandardsInputs Inputs(
        bool hasDescription = false,
        bool hasLogo = false,
        bool hasLinks = false,
        bool hasArticulatedProduct = false,
        int memberCount = 0,
        bool hasFounder = false,
        bool hasPitchDeck = false,
        int roadmapItemCount = 0,
        IReadOnlyDictionary<CommunityDocumentType, Guid>? documents = null)
        => new(
            StartupId: Guid.NewGuid(),
            HasDescription: hasDescription,
            HasLogo: hasLogo,
            HasLinks: hasLinks,
            HasArticulatedProduct: hasArticulatedProduct,
            MemberCount: memberCount,
            HasFounder: hasFounder,
            HasPitchDeck: hasPitchDeck,
            RoadmapItemCount: roadmapItemCount,
            Documents: documents ?? new Dictionary<CommunityDocumentType, Guid>());

    private static CommunityStandardsInputs FullyCompliantInputs() => InputsWithFirst(12);

    /// <summary>
    /// Builds inputs that satisfy exactly <paramref name="count"/> checks, filling the seven profile
    /// signals first and then the five documents, so the level thresholds can be walked precisely.
    /// </summary>
    private static CommunityStandardsInputs InputsWithFirst(int count)
    {
        Dictionary<CommunityDocumentType, Guid> documents = [];
        for (int i = 0; i < count - 7 && i < AllDocumentTypes.Length; i++)
        {
            documents[AllDocumentTypes[i]] = Guid.NewGuid();
        }

        return Inputs(
            hasDescription: count >= 1,
            hasLogo: count >= 2,
            hasLinks: count >= 3,
            hasArticulatedProduct: count >= 4,
            memberCount: count >= 5 ? 2 : 0,
            hasFounder: count >= 5,
            hasPitchDeck: count >= 6,
            roadmapItemCount: count >= 7 ? 3 : 0,
            documents: documents);
    }
}
