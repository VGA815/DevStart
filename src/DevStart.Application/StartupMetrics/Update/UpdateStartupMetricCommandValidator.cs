using DevStart.Domain.StartupMetrics;
using FluentValidation;

namespace DevStart.Application.StartupMetrics.Update
{
    internal sealed class UpdateStartupMetricCommandValidator : AbstractValidator<UpdateStartupMetricCommand>
    {
        public UpdateStartupMetricCommandValidator()
        {
            RuleFor(x => x.StartupId).NotEmpty();
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.MetricType).IsInEnum();
            RuleFor(x => x.Value).NotNull();
            // Absolute metrics can't be negative; growth metrics legitimately can (decline).
            RuleFor(x => x.Value)
                .GreaterThanOrEqualTo(0)
                .When(x => x.MetricType is not (MetricType.MomGrowth or MetricType.GrowthRate));
        }
    }
}
