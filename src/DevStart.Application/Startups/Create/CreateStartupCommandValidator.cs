using FluentValidation;

namespace DevStart.Application.Startups.Create
{
    internal sealed class CreateStartupCommandValidator : AbstractValidator<CreateStartupCommand>
    {
        public CreateStartupCommandValidator()
        {
            RuleFor(s => s.Name).NotEmpty();
            RuleFor(s => s.PublicEmail).NotEmpty();
            RuleFor(s => s.Stage).IsInEnum();
            RuleFor(s => s.Stack).NotEmpty();
            RuleFor(s => s.ProductValueProposition).NotEmpty();
            RuleFor(s => s.ProductProblemSolution).NotEmpty();
            RuleFor(s => s.ProductDifferentiators).NotEmpty();
            RuleFor(s => s.ProductName).NotEmpty();
            RuleFor(s => s.Tam).GreaterThanOrEqualTo(0).When(s => s.Tam.HasValue);
            RuleFor(s => s.Sam).GreaterThanOrEqualTo(0).When(s => s.Sam.HasValue);
            RuleFor(s => s.Som).GreaterThanOrEqualTo(0).When(s => s.Som.HasValue);
            RuleFor(s => s.MarketGrowthRate).GreaterThanOrEqualTo(0).When(s => s.MarketGrowthRate.HasValue);
            RuleFor(s => s.Industry).IsInEnum();
            RuleFor(s => s.TargetRoundAmount).GreaterThanOrEqualTo(0).When(s => s.TargetRoundAmount.HasValue);
        }
    }
}