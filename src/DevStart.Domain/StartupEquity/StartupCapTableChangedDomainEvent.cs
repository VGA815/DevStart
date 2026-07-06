using DevStart.SharedKernel;

namespace DevStart.Domain.StartupEquity
{
    /// <summary>Raised when a startup's founding cap table is replaced, so derived caches (e.g. the
    /// startup score) can be invalidated and downstream consumers can react.</summary>
    public sealed record StartupCapTableChangedDomainEvent(Guid StartupId) : IDomainEvent;
}
