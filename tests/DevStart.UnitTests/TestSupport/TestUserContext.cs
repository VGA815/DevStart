using DevStart.Application.Abstractions.Authentication;

namespace DevStart.UnitTests.TestSupport;

internal sealed class TestUserContext(Guid userId, Guid? sessionId = null) : IUserContext
{
    public Guid UserId { get; } = userId;

    public Guid? SessionId { get; } = sessionId;
}
