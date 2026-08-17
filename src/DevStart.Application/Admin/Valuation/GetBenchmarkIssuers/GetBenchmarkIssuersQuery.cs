using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Admin.Valuation.GetBenchmarkIssuers
{
    /// <summary>
    /// Lists the curated comparables, inactive ones included — a delisted issuer stays visible so the
    /// reason it was dropped stays visible with it.
    /// </summary>
    public sealed record GetBenchmarkIssuersQuery : IQuery<List<BenchmarkIssuerResponse>>;
}
