using DevStart.Application.Abstractions.Authentication;

namespace DevStart.UnitTests.TestSupport;

internal sealed class TestUserContext(Guid userId) : IUserContext
{
    public Guid UserId { get; } = userId;
}
