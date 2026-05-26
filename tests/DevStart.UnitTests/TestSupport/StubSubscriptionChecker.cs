using DevStart.Application.Abstractions.Subscriptions;

namespace DevStart.UnitTests.TestSupport
{
    internal sealed class StubSubscriptionChecker(bool hasActivePro) : ISubscriptionChecker
    {
        public Task<bool> HasActiveProAsync(Guid userId, CancellationToken ct) => Task.FromResult(hasActivePro);
    }
}
