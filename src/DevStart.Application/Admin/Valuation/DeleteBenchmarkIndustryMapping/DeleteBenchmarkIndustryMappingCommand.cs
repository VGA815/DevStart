using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Admin.Valuation.DeleteBenchmarkIndustryMapping
{
    /// <summary>
    /// Removes a mapping outright. Safe to delete — unlike an issuer, a mapping owns no observations;
    /// dropping one simply returns its bucket to the unmapped work queue.
    /// </summary>
    public sealed record DeleteBenchmarkIndustryMappingCommand(Guid Id) : ICommand;
}
