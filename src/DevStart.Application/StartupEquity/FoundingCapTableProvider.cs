using DevStart.Application.Abstractions.Data;
using DevStart.Domain.StartupEquity;
using DevStart.Domain.StartupMembers;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.StartupEquity
{
    internal sealed class FoundingCapTableProvider(IApplicationDbContext context) : IFoundingCapTableProvider
    {
        /// <summary>Default ESOP reservation used when a startup has no explicit cap table yet.</summary>
        public const decimal DefaultEsopPercentage = 10m;

        public async Task<IReadOnlyList<FoundingCapTableHolder>> GetEffectiveHoldersAsync(
            Guid startupId,
            CancellationToken cancellationToken)
        {
            // Persisted cap table takes precedence. Left-join Profiles so a founder without a profile
            // row still gets a sensible display name.
            List<FoundingCapTableHolder> persisted = await (
                from h in context.StartupEquityHolders.AsNoTracking()
                where h.StartupId == startupId
                join p in context.Profiles.AsNoTracking() on h.ProfileId equals p.UserId into profiles
                from profile in profiles.DefaultIfEmpty()
                orderby h.HolderType, h.CreatedAt
                select new FoundingCapTableHolder(
                    h.ProfileId,
                    h.HolderType,
                    h.Name ?? (profile != null ? profile.Name : null) ?? string.Empty,
                    h.EquityPercentage,
                    h.VestingStartDate,
                    h.VestingMonths,
                    h.CliffMonths))
                .ToListAsync(cancellationToken);

            if (persisted.Count > 0)
            {
                // Fill any blank display names deterministically (founders without a profile row).
                return NameBlankHolders(persisted);
            }

            return await BootstrapDefaultAsync(startupId, cancellationToken);
        }

        // MVP default: founders split (100 - default ESOP) equally, first founder absorbs the rounding
        // residual, plus a default ESOP pool row. Not persisted — a starting point for editing.
        private async Task<IReadOnlyList<FoundingCapTableHolder>> BootstrapDefaultAsync(
            Guid startupId,
            CancellationToken cancellationToken)
        {
            var founders = await (
                from sm in context.StartupMembers.AsNoTracking()
                join p in context.Profiles.AsNoTracking() on sm.ProfileId equals p.UserId into profiles
                from profile in profiles.DefaultIfEmpty()
                where sm.StartupId == startupId && sm.Role == StartupRole.Founder
                orderby sm.CreatedAt
                select new { sm.ProfileId, Name = profile != null ? profile.Name : null })
                .ToListAsync(cancellationToken);

            const decimal foundersPoolPct = 100m - DefaultEsopPercentage;
            var holders = new List<FoundingCapTableHolder>();

            if (founders.Count == 0)
            {
                holders.Add(new FoundingCapTableHolder(
                    null, EquityHolderType.Founder, "Founders pool", foundersPoolPct, null, null, null));
            }
            else
            {
                decimal perFounder = Math.Round(foundersPoolPct / founders.Count, 2, MidpointRounding.AwayFromZero);
                decimal residual = foundersPoolPct - (perFounder * founders.Count);
                for (int i = 0; i < founders.Count; i++)
                {
                    decimal share = i == 0 ? perFounder + residual : perFounder;
                    string name = string.IsNullOrWhiteSpace(founders[i].Name)
                        ? $"Founder {i + 1}"
                        : founders[i].Name!;
                    holders.Add(new FoundingCapTableHolder(
                        founders[i].ProfileId, EquityHolderType.Founder, name, share, null, null, null));
                }
            }

            holders.Add(new FoundingCapTableHolder(
                null, EquityHolderType.Esop, "ESOP pool", DefaultEsopPercentage, null, null, null));

            return holders;
        }

        private static IReadOnlyList<FoundingCapTableHolder> NameBlankHolders(List<FoundingCapTableHolder> holders)
        {
            int founderIndex = 0;
            for (int i = 0; i < holders.Count; i++)
            {
                FoundingCapTableHolder h = holders[i];
                if (h.HolderType == EquityHolderType.Founder)
                {
                    founderIndex++;
                }

                if (!string.IsNullOrWhiteSpace(h.Name))
                {
                    continue;
                }

                string fallback = h.HolderType switch
                {
                    EquityHolderType.Founder => $"Founder {founderIndex}",
                    EquityHolderType.Esop => "ESOP pool",
                    EquityHolderType.Advisor => "Advisor",
                    _ => "Holder"
                };
                holders[i] = h with { Name = fallback };
            }

            return holders;
        }
    }
}
