using DevStart.Domain.Users;
using Shouldly;

namespace DevStart.UnitTests.Domain.Users;

public sealed class UserTests
{
    [Fact]
    public void Create_ShouldInitializeUserWithDefaults()
    {
        DateTime createdAt = new(2026, 5, 16, 10, 0, 0, DateTimeKind.Utc);

        User user = User.Create("alice", "alice@example.com", "hash", createdAt);

        user.Id.ShouldNotBe(Guid.Empty);
        user.Username.ShouldBe("alice");
        user.Email.ShouldBe("alice@example.com");
        user.PasswordHash.ShouldBe("hash");
        user.CreatedAt.ShouldBe(createdAt);
        user.UpdatedAt.ShouldBe(createdAt);
        user.IsVerified.ShouldBeFalse();
        user.Role.ShouldBe(UserSystemRole.User);
    }
}
