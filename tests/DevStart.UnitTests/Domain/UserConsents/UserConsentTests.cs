using DevStart.Domain.UserConsents;
using Shouldly;

namespace DevStart.UnitTests.Domain.UserConsents;

public sealed class UserConsentTests
{
    [Fact]
    public void Create_ShouldInitializeActiveConsent()
    {
        Guid userId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        DateTime acceptedAt = new(2026, 5, 16, 10, 0, 0, DateTimeKind.Utc);

        UserConsent consent = UserConsent.Create(userId, ConsentType.PrivacyPolicy, "v1", acceptedAt);

        consent.Id.ShouldNotBe(Guid.Empty);
        consent.UserId.ShouldBe(userId);
        consent.Type.ShouldBe(ConsentType.PrivacyPolicy);
        consent.DocumentVersion.ShouldBe("v1");
        consent.AcceptedAt.ShouldBe(acceptedAt);
        consent.RevokedAt.ShouldBeNull();
        consent.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Revoke_ShouldSetRevokedAtAndDeactivateConsent()
    {
        UserConsent consent = UserConsent.Create(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            ConsentType.TermsOfService,
            "v1",
            new DateTime(2026, 5, 16, 10, 0, 0, DateTimeKind.Utc));
        DateTime revokedAt = new(2026, 5, 16, 11, 0, 0, DateTimeKind.Utc);

        consent.Revoke(revokedAt);

        consent.RevokedAt.ShouldBe(revokedAt);
        consent.IsActive.ShouldBeFalse();
    }
}
