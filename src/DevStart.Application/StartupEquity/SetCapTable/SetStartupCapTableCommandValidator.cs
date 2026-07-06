using DevStart.Domain.StartupEquity;
using FluentValidation;

namespace DevStart.Application.StartupEquity.SetCapTable
{
    internal sealed class SetStartupCapTableCommandValidator : AbstractValidator<SetStartupCapTableCommand>
    {
        // Absorb rounding noise from per-row percentages while still rejecting real mistakes.
        private const decimal SumTolerance = 0.01m;

        public SetStartupCapTableCommandValidator()
        {
            RuleFor(x => x.StartupId).NotEmpty();

            RuleFor(x => x.Holders)
                .NotEmpty()
                .Must(SumsTo100)
                    .WithMessage("The equity percentages of all holders must sum to exactly 100%.")
                .Must(HasNoDuplicateProfiles)
                    .WithMessage("A profile may appear at most once on the cap table.");

            RuleForEach(x => x.Holders).ChildRules(holder =>
            {
                holder.RuleFor(h => h.HolderType).IsInEnum();

                holder.RuleFor(h => h.EquityPercentage).InclusiveBetween(0m, 100m);

                // Founder rows must identify the founder; other rows must carry a display name.
                holder.RuleFor(h => h.ProfileId)
                    .NotEmpty()
                    .When(h => h.HolderType == EquityHolderType.Founder)
                    .WithMessage("Founder rows must reference a profile.");

                holder.RuleFor(h => h.Name)
                    .NotEmpty()
                    .When(h => h.HolderType != EquityHolderType.Founder)
                    .WithMessage("Non-founder rows must have a name.");

                holder.RuleFor(h => h)
                    .Must(HasConsistentVesting)
                    .WithMessage("Vesting needs a start date and a positive duration, and the cliff must not exceed the duration.");
            });
        }

        private static bool SumsTo100(IReadOnlyList<CapTableHolderInput> holders)
        {
            decimal sum = holders.Sum(h => h.EquityPercentage);
            return Math.Abs(sum - 100m) <= SumTolerance;
        }

        private static bool HasNoDuplicateProfiles(IReadOnlyList<CapTableHolderInput> holders)
        {
            var seen = new HashSet<Guid>();
            foreach (CapTableHolderInput h in holders)
            {
                if (h.ProfileId is { } id && !seen.Add(id))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool HasConsistentVesting(CapTableHolderInput h)
        {
            bool any = h.VestingStartDate.HasValue || h.VestingMonths.HasValue || h.CliffMonths.HasValue;
            if (!any)
            {
                return true;
            }

            if (!h.VestingStartDate.HasValue || h.VestingMonths is not > 0)
            {
                return false;
            }

            int cliff = h.CliffMonths ?? 0;
            return cliff >= 0 && cliff <= h.VestingMonths.Value;
        }
    }
}
