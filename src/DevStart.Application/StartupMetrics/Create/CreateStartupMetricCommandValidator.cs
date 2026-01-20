using FluentValidation;

namespace DevStart.Application.StartupMetrics.Create
{
    internal sealed class CreateStartupMetricCommandValidator : AbstractValidator<CreateStartupMetricCommand>
    {
        public CreateStartupMetricCommandValidator()
        {
            RuleFor(x => x.StartupId).NotEmpty();
            RuleFor(x => x.Value).NotNull();
            RuleFor(x => x.MetricType).IsInEnum();
        }
    }
}
