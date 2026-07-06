using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.StartupEquity.GetCapTable
{
    // Founder/admin-gated, so deliberately NOT ICacheableQuery — caching an authorization-scoped
    // result would risk serving it to the wrong caller.
    public sealed record GetStartupCapTableQuery(Guid StartupId) : IQuery<StartupCapTableResponse>;
}
