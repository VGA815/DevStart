using FluentValidation;

namespace DevStart.Application.Admin.Startups.BanStartup
{
    internal sealed class BanStartupCommandValidator : AbstractValidator<BanStartupCommand>
    {
        public BanStartupCommandValidator()
        {
            RuleFor(c => c.StartupId).NotEmpty();
            RuleFor(c => c.Reason)
                .NotEmpty()
                .MinimumLength(3)
                .MaximumLength(1000);
        }
    }
}
