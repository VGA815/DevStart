using FluentValidation;

namespace DevStart.Application.StartupMetrics.Update
{
    internal sealed class UpdateStartupMetricCommandValidator : AbstractValidator<UpdateStartupMetricCommand>
    {
        public UpdateStartupMetricCommandValidator()
        {
            RuleFor(x => x.StartupId).NotEmpty();
            RuleFor(x => x.CreatedAt).NotEmpty();
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.MetricType).NotEmpty().IsInEnum();
            RuleFor(x => x.Value).NotEmpty();
        }
    }
}
