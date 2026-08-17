using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Admin;
using DevStart.Domain.Valuation;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Admin.Valuation.SaveBenchmarkIssuer
{
    internal sealed class SaveBenchmarkIssuerCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IDateTimeProvider dateTimeProvider)
        : ICommandHandler<SaveBenchmarkIssuerCommand, Guid>
    {
        public async Task<Result<Guid>> Handle(SaveBenchmarkIssuerCommand command, CancellationToken cancellationToken)
        {
            string ticker = command.Ticker.Trim().ToUpperInvariant();
            DateTime now = dateTimeProvider.UtcNow;
            Guid? editedId = command.Id;

            bool tickerTaken = await context.BenchmarkIssuers
                .AnyAsync(i => i.Ticker == ticker && (editedId == null || i.Id != editedId), cancellationToken);
            if (tickerTaken)
            {
                return Result.Failure<Guid>(ValuationBenchmarkErrors.DuplicateIssuerTicker);
            }

            BenchmarkIssuer issuer;
            if (editedId is { } id)
            {
                BenchmarkIssuer? existing = await context.BenchmarkIssuers
                    .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
                if (existing is null)
                {
                    return Result.Failure<Guid>(ValuationBenchmarkErrors.IssuerNotFound);
                }

                issuer = existing;
            }
            else
            {
                issuer = new BenchmarkIssuer { Id = Guid.NewGuid(), CreatedAt = now };
                context.BenchmarkIssuers.Add(issuer);
            }

            issuer.Ticker = ticker;
            issuer.Update(
                Normalize(command.Inn),
                command.DisplayName.Trim(),
                command.Industry,
                command.IsActive,
                command.RevenueOverride,
                command.RevenueOverrideFiscalYear,
                Normalize(command.RevenueOverrideNote),
                Normalize(command.Note),
                now);

            context.AdminActionLogs.Add(AdminActionLog.Create(
                userContext.UserId,
                AdminActionType.SaveBenchmarkIssuer,
                AdminTargetType.BenchmarkIssuer,
                issuer.Id,
                $"{(editedId is null ? "Created" : "Updated")} benchmark issuer {ticker} "
                    + $"({command.Industry}, active={command.IsActive})"
                    + (command.RevenueOverride is { } r
                        ? $", revenue override {r} FY{command.RevenueOverrideFiscalYear}"
                        : string.Empty),
                now));

            await context.SaveChangesAsync(cancellationToken);

            // Deliberately no cache eviction: the registry feeds the collection jobs and the derivation
            // preview, neither of which the valuation engine reads. Nothing cached under
            // CacheKeys.ValuationBenchmarks or the startup prefix can have changed.
            return issuer.Id;
        }

        private static string? Normalize(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
