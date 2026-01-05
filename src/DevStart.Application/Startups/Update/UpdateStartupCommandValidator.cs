using FluentValidation;

namespace DevStart.Application.Startups.Update
{
    internal sealed class UpdateStartupCommandValidator : AbstractValidator<UpdateStartupCommand>
    {
        public UpdateStartupCommandValidator()
        {
            RuleFor(s => s.IsStopped).NotEmpty();
            RuleFor(s => s.SocialMediaLinks).NotEmpty();
            RuleFor(s => s.PublicEmail).NotEmpty();
            RuleFor(s => s.AvatarUrl).NotEmpty();
            RuleFor(s => s.BillingEmail).NotEmpty();
            RuleFor(s => s.Description).NotEmpty();
            RuleFor(s => s.Name).NotEmpty();
            RuleFor(s => s.Stage).NotEmpty().IsInEnum();
        }
    }
}
