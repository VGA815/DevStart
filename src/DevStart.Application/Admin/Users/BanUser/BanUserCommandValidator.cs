using FluentValidation;

namespace DevStart.Application.Admin.Users.BanUser
{
    internal sealed class BanUserCommandValidator : AbstractValidator<BanUserCommand>
    {
        public BanUserCommandValidator()
        {
            RuleFor(c => c.UserId).NotEmpty();
            RuleFor(c => c.Reason)
                .NotEmpty()
                .MinimumLength(3)
                .MaximumLength(1000);
        }
    }
}
