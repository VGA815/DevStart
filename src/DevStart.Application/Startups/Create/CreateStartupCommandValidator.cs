using FluentValidation;

namespace DevStart.Application.Startups.Create
{
    /// <summary>
    /// Creation asks for the minimum that makes a startup a startup: identity, a reachable email,
    /// a stage and what the product does. Stack, value proposition and differentiators are scoring
    /// inputs, not gates — requiring them at sign-up only pushed founders into filling them with
    /// noise. They are prompted for again by the scoring hints once the startup exists.
    /// </summary>
    internal sealed class CreateStartupCommandValidator : AbstractValidator<CreateStartupCommand>
    {
        public CreateStartupCommandValidator()
        {
            RuleFor(s => s.Name).NotEmpty();
            RuleFor(s => s.PublicEmail).NotEmpty().EmailAddress();
            RuleFor(s => s.Stage).IsInEnum();
            RuleFor(s => s.Location).IsInEnum().When(s => s.Location.HasValue);
            RuleFor(s => s.ProductSolution).NotEmpty();
            RuleFor(s => s.Tam).GreaterThanOrEqualTo(0).When(s => s.Tam.HasValue);
            RuleFor(s => s.Sam).GreaterThanOrEqualTo(0).When(s => s.Sam.HasValue);
            RuleFor(s => s.Som).GreaterThanOrEqualTo(0).When(s => s.Som.HasValue);
            RuleFor(s => s.MarketGrowthRate).GreaterThanOrEqualTo(0).When(s => s.MarketGrowthRate.HasValue);
            RuleFor(s => s.Industry).IsInEnum();
            RuleFor(s => s.TargetRoundAmount).GreaterThanOrEqualTo(0).When(s => s.TargetRoundAmount.HasValue);
        }
    }
}
