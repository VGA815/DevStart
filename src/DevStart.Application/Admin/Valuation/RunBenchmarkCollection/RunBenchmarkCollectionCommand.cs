using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Admin.Valuation.RunBenchmarkCollection
{
    /// <summary>Which collector to run on demand.</summary>
    public enum BenchmarkCollectionKind
    {
        MarketCap = 0,
        Revenue = 1,
        Both = 2,
    }

    /// <summary>
    /// Queues a collection run now rather than at the next quarter boundary. Queues, not runs: the work
    /// is minutes of outbound HTTP and belongs on the job server, not on an admin's request thread.
    /// </summary>
    public sealed record RunBenchmarkCollectionCommand(BenchmarkCollectionKind Kind) : ICommand;
}
