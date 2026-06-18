using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Scoring;

namespace DevStart.Application.Startups.GetScore
{
    // Public, authorization-gated entry point for a startup's score. NOT cacheable: the Pro/member
    // gate in the handler must run on every request. The actual computation is cached one layer down
    // via ComputeStartupScoreQuery (viewer-independent), so the gate can never be skipped on a cache hit.
    public sealed record GetStartupScoreQuery(Guid StartupId) : IQuery<ScoreResult>;
}
