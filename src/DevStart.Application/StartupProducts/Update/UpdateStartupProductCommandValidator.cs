using FluentValidation;

namespace DevStart.Application.StartupProducts.Update
{
    /// <summary>
    /// Mirrors <see cref="Startups.Create.CreateStartupCommandValidator"/>: only the solution is a
    /// gate, because it is the one non-nullable column on the product. Problem, value proposition
    /// and differentiators are scoring inputs — a founder must be able to save a partly filled
    /// product rather than be blocked out of the editor by fields creation never asked for.
    /// </summary>
    internal sealed class UpdateStartupProductCommandValidator : AbstractValidator<UpdateStartupProductCommand>
    {
        public UpdateStartupProductCommandValidator()
        {
            RuleFor(x => x.StartupId).NotEmpty();
            RuleFor(x => x.Solution).NotEmpty();
        }
    }
}
