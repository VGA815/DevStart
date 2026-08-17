using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Admin;
using DevStart.Domain.Valuation;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Admin.Valuation.SaveBenchmarkIndustryMapping
{
    internal sealed class SaveBenchmarkIndustryMappingCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IDateTimeProvider dateTimeProvider)
        : ICommandHandler<SaveBenchmarkIndustryMappingCommand, Guid>
    {
        public async Task<Result<Guid>> Handle(
            SaveBenchmarkIndustryMappingCommand command,
            CancellationToken cancellationToken)
        {
            string key = command.ExternalKey.Trim();
            string comparisonKey = key.ToLowerInvariant();
            DateTime now = dateTimeProvider.UtcNow;
            BenchmarkMappingSourceKind sourceKind = command.SourceKind;

            // Matched case-insensitively, which is what makes the upsert agree with the functional
            // unique index on lower(external_key) and with the OrdinalIgnoreCase lookups every reader
            // builds. Without this, "Retail (Online)" and "retail (online)" would race to insert, one
            // would hit the constraint, and a casing change would read as a conflict rather than an
            // edit. The stored value keeps the casing the admin typed — it is what the UI displays.
            BenchmarkIndustryMapping? mapping = await context.BenchmarkIndustryMappings
                .FirstOrDefaultAsync(
                    m => m.SourceKind == sourceKind && m.ExternalKey.ToLower() == comparisonKey,
                    cancellationToken);

            if (mapping is null)
            {
                mapping = BenchmarkIndustryMapping.Create(
                    sourceKind, key, command.Industry, Normalize(command.Note), now);
                context.BenchmarkIndustryMappings.Add(mapping);
            }
            else
            {
                mapping.ExternalKey = key;
                mapping.Update(command.Industry, Normalize(command.Note), now);
            }

            context.AdminActionLogs.Add(AdminActionLog.Create(
                userContext.UserId,
                AdminActionType.SaveBenchmarkIndustryMapping,
                AdminTargetType.BenchmarkIndustryMapping,
                mapping.Id,
                $"Mapped {command.SourceKind} bucket \"{key}\" to "
                    + (command.Industry is { } industry ? industry.ToString() : "no sector (excluded)"),
                now));

            await context.SaveChangesAsync(cancellationToken);

            return mapping.Id;
        }

        private static string? Normalize(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
