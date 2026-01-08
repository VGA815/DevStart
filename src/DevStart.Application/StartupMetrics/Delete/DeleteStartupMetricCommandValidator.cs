using FluentValidation;

namespace DevStart.Application.StartupMetrics.Delete
{
    internal sealed class DeleteStartupMetricCommandValidator : AbstractValidator<DeleteStartupMetricCommand>
    {
        public DeleteStartupMetricCommandValidator()
        {
            RuleFor(x => x.MetricId).NotEmpty();
        }
    }
}
