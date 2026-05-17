using DevStart.Domain.EmailVerificationTokens;
using Shouldly;

namespace DevStart.UnitTests.Domain.Tokens;

public sealed class EmailVerificationTokenTests
{
    [Fact]
    public void Create_ShouldInitializeEmailVerificationToken()
    {
        Guid userId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        DateTime createdAt = new(2026, 5, 16, 10, 0, 0, DateTimeKind.Utc);
        DateTime expiresAt = createdAt.AddHours(1);

        EmailVerificationToken token = EmailVerificationToken.Create(userId, createdAt, expiresAt);

        token.TokenId.ShouldNotBe(Guid.Empty);
        token.UserId.ShouldBe(userId);
        token.CreatedAt.ShouldBe(createdAt);
        token.ExpiresAt.ShouldBe(expiresAt);
    }
}
