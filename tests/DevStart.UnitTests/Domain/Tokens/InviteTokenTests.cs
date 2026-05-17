using DevStart.Domain.InviteTokens;
using Shouldly;

namespace DevStart.UnitTests.Domain.Tokens;

public sealed class InviteTokenTests
{
    [Fact]
    public void Create_ShouldInitializeUnusedInviteToken()
    {
        Guid startupId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        DateTime expiresAt = new(2026, 5, 17, 10, 0, 0, DateTimeKind.Utc);

        InviteToken token = InviteToken.Create(startupId, expiresAt);

        token.Id.ShouldNotBe(Guid.Empty);
        token.StartupId.ShouldBe(startupId);
        token.ExpiresAt.ShouldBe(expiresAt);
        token.IsUsed.ShouldBeFalse();
    }
}
