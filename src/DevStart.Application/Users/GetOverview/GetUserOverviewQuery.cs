using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Users.GetOverview
{
    // Public entry point for the aggregated user overview. NOT cacheable: the viewer-dependent
    // redaction (owner-only Email and TotalInvestedAmount) must run on every request. The actual
    // read is cached one layer down via FetchUserOverviewQuery (viewer-independent, full aggregate),
    // so a warm cache can never let one user read another user's private fields.
    public sealed record GetUserOverviewQuery(Guid UserId) : IQuery<UserOverviewResponse>;
}
