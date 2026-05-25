using FluentValidation;

namespace DevStart.Application.Users.ChangePassword
{
    internal sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
    {
        public ChangePasswordCommandValidator()
        {
            RuleFor(c => c.CurrentPassword).NotEmpty();
            RuleFor(c => c.NewPassword).NotEmpty().MinimumLength(8);
        }
    }
}
