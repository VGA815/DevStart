using FluentValidation;

namespace DevStart.Application.StartupProducts.Update
{
    internal sealed class UpdateStartupProductCommandValidator : AbstractValidator<UpdateStartupProductCommand>
    {
        public UpdateStartupProductCommandValidator()
        {
            RuleFor(x => x.Differentiators).NotEmpty();
            RuleFor(x => x.StartupId).NotEmpty();
            RuleFor(x => x.ValueProposition).NotEmpty();
            RuleFor(x => x.Solution).NotEmpty();
            RuleFor(x => x.Problem).NotEmpty();
        }
    }
}
