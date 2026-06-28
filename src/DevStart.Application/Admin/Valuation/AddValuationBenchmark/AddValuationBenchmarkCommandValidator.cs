using DevStart.Domain.Valuation;
using FluentValidation;

namespace DevStart.Application.Admin.Valuation.AddValuationBenchmark
{
    internal sealed class AddValuationBenchmarkCommandValidator : AbstractValidator<AddValuationBenchmarkCommand>
    {
        public AddValuationBenchmarkCommandValidator()
        {
            RuleFor(c => c.MetricType).IsInEnum();
            RuleFor(c => c.Industry).IsInEnum();

            RuleFor(c => c.Value)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Benchmark value must be non-negative.");

            RuleFor(c => c.EffectiveFrom)
                .NotEmpty()
                .WithMessage("EffectiveFrom is required.");

            RuleFor(c => c.Source)
                .NotEmpty()
                .MaximumLength(512)
                .WithMessage("A non-empty source (≤ 512 chars) is required for every benchmark.");

            // Median rows are stage-scoped and carry a currency.
            When(c => c.MetricType == BenchmarkMetricType.PreMoneyMedian, () =>
            {
                RuleFor(c => c.Stage)
                    .NotNull()
                    .WithMessage("A pre-money median must specify a stage.");
                RuleFor(c => c.Stage!.Value)
                    .IsInEnum()
                    .When(c => c.Stage.HasValue);
                RuleFor(c => c.Currency)
                    .Equal("RUB")
                    .WithMessage("A pre-money median must be in RUB.");
            });

            // Revenue multiples are sector-only and dimensionless.
            When(c => c.MetricType == BenchmarkMetricType.RevenueMultiple, () =>
            {
                RuleFor(c => c.Stage)
                    .Null()
                    .WithMessage("A revenue multiple is sector-only and must not specify a stage.");
                RuleFor(c => c.Currency)
                    .Null()
                    .WithMessage("A revenue multiple is dimensionless and must not carry a currency.");
            });
        }
    }
}
