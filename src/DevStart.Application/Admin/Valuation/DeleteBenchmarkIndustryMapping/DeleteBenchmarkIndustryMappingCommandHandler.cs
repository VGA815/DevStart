using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Admin;
using DevStart.Domain.Valuation;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Admin.Valuation.DeleteBenchmarkIndustryMapping
{
    internal sealed class DeleteBenchmarkIndustryMappingCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IDateTimeProvider dateTimeProvider)
        : ICommandHandler<DeleteBenchmarkIndustryMappingCommand>
    {
        public async Task<Result> Handle(
            DeleteBenchmarkIndustryMappingCommand command,
            CancellationToken cancellationToken)
        {
            BenchmarkIndustryMapping? mapping = await context.BenchmarkIndustryMappings
                .FirstOrDefaultAsync(m => m.Id == command.Id, cancellationToken);

            if (mapping is null)
            {
                return Result.Failure(ValuationBenchmarkErrors.MappingNotFound);
            }

            DateTime now = dateTimeProvider.UtcNow;

            context.BenchmarkIndustryMappings.Remove(mapping);
            context.AdminActionLogs.Add(AdminActionLog.Create(
                userContext.UserId,
                AdminActionType.DeleteBenchmarkIndustryMapping,
                AdminTargetType.BenchmarkIndustryMapping,
                mapping.Id,
                $"Removed {mapping.SourceKind} mapping for \"{mapping.ExternalKey}\"",
                now));

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
