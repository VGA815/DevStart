using FluentValidation;

namespace DevStart.Application.StartupMetrics.Create
{
    internal sealed class CreateStartupMetricCommandValidator : AbstractValidator<CreateStartupMetricCommand>
    {
        public CreateStartupMetricCommandValidator()
        {
            RuleFor(x => x.StartupId).NotEmpty();
            RuleFor(x => x.Value).NotEmpty();
            RuleFor(x => x.MetricType).NotEmpty().IsInEnum();
        }
    }
}
