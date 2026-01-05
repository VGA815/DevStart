using FluentValidation;

namespace DevStart.Application.Startups.Create
{
    internal sealed class CreateStartupCommandValidator : AbstractValidator<CreateStartupCommand>
    {
        public CreateStartupCommandValidator()
        {
            RuleFor(s => s.Name).NotEmpty();
            RuleFor(s => s.Description).NotEmpty();
            RuleFor(s => s.SocialMediaLinks).NotEmpty();
            RuleFor(s => s.PublicEmail).NotEmpty();
            RuleFor(s => s.BillingEmail).NotEmpty();
            RuleFor(s => s.AvatarUrl).NotEmpty();
            RuleFor(s => s.Stage).NotEmpty().IsInEnum();
            RuleFor(s => s.IsStopped).NotEmpty();
            RuleFor(s => s.Location).NotEmpty();
            RuleFor(s => s.Stack).NotEmpty();
            RuleFor(s => s.ProductValueProposition).NotEmpty();
            RuleFor(s => s.ProductProblemSolution).NotEmpty();
            RuleFor(s => s.ProductDifferentiators).NotEmpty();
            RuleFor(s => s.ProductName).NotEmpty();
        }
    }
}