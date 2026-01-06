using FluentValidation;

namespace DevStart.Application.Startups.Delete
{
    internal sealed class DeleteStartupCommandValidator : AbstractValidator<DeleteStartupCommand>
    {
        public DeleteStartupCommandValidator()
        {
            RuleFor(s => s.StartupId).NotEmpty();
        }
    }
}
