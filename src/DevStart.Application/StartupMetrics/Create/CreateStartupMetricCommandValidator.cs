using DevStart.Domain.StartupMetrics;
using FluentValidation;

namespace DevStart.Application.StartupMetrics.Create
{
    internal sealed class CreateStartupMetricCommandValidator : AbstractValidator<CreateStartupMetricCommand>
    {
        public CreateStartupMetricCommandValidator()
        {
            RuleFor(x => x.StartupId).NotEmpty();
            RuleFor(x => x.Value).NotNull();
            // Absolute metrics can't be negative; growth metrics legitimately can (decline).
            RuleFor(x => x.Value)
                .GreaterThanOrEqualTo(0)
                .When(x => x.MetricType is not (MetricType.MomGrowth or MetricType.GrowthRate));
            RuleFor(x => x.MetricType).IsInEnum();
        }
    }
}
