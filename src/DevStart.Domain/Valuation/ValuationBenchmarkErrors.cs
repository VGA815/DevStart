using DevStart.SharedKernel;

namespace DevStart.Domain.Valuation
{
    public static class ValuationBenchmarkErrors
    {
        public static readonly Error DuplicateVersion = Error.Conflict(
            "ValuationBenchmarks.DuplicateVersion",
            "A benchmark for this metric, sector, stage and effective date already exists. " +
            "Add a correction as a new version with a later effective date.");

        public static readonly Error IssuerNotFound = Error.NotFound(
            "BenchmarkIssuers.NotFound",
            "Benchmark issuer not found.");

        public static readonly Error DuplicateIssuerTicker = Error.Conflict(
            "BenchmarkIssuers.DuplicateTicker",
            "An issuer with this MOEX ticker already exists. Edit the existing row instead.");

        public static readonly Error MappingNotFound = Error.NotFound(
            "BenchmarkIndustryMappings.NotFound",
            "Benchmark industry mapping not found.");

        public static readonly Error DuplicateMappingKey = Error.Conflict(
            "BenchmarkIndustryMappings.DuplicateKey",
            "This external key is already mapped for this source. Edit the existing mapping instead.");

        /// <summary>
        /// The parse is all-or-nothing: a partial import is worse than a refusal, because it produces a
        /// plausible but incomplete set that nobody can tell apart from a complete one.
        /// </summary>
        public static Error DamodaranLayoutUnrecognised(string expected, string found) => Error.Problem(
            "Damodaran.LayoutUnrecognised",
            $"Could not find the expected columns in the uploaded dataset. Expected one of: {expected}. " +
            $"Found: {found}. Nothing was imported.");

        public static readonly Error DamodaranEmptyDataset = Error.Problem(
            "Damodaran.EmptyDataset",
            "The uploaded dataset contained no parsable industry rows. Nothing was imported.");
    }
}
