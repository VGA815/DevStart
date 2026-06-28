using DevStart.SharedKernel;

namespace DevStart.Domain.Valuation
{
    public static class ValuationBenchmarkErrors
    {
        public static readonly Error DuplicateVersion = Error.Conflict(
            "ValuationBenchmarks.DuplicateVersion",
            "A benchmark for this metric, sector, stage and effective date already exists. " +
            "Add a correction as a new version with a later effective date.");
    }
}
