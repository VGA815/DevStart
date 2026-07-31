using DevStart.Application.Abstractions.ServiceOrders;
using DevStart.Domain.ServiceOrders;

namespace DevStart.UnitTests.TestSupport
{
    /// <summary>
    /// Test double for <see cref="IServiceEntitlementChecker"/>. Grants nothing by default; add
    /// (serviceType, targetId) pairs via <see cref="Grant"/> to simulate a paid one-time service.
    /// </summary>
    internal sealed class StubServiceEntitlementChecker : IServiceEntitlementChecker
    {
        private readonly HashSet<(ServiceType, Guid)> _granted = [];

        public int InvalidateCount { get; private set; }

        public StubServiceEntitlementChecker Grant(ServiceType serviceType, Guid targetId)
        {
            _granted.Add((serviceType, targetId));
            return this;
        }

        public Task<bool> HasAsync(Guid userId, ServiceType serviceType, Guid targetId, CancellationToken ct)
            => Task.FromResult(_granted.Contains((serviceType, targetId)));

        public Task InvalidateAsync(Guid userId, CancellationToken ct)
        {
            InvalidateCount++;
            return Task.CompletedTask;
        }
    }
}
