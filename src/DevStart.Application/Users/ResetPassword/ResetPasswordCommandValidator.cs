using FluentValidation;

namespace DevStart.Application.Users.ResetPassword
{
    internal sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
    {
        public ResetPasswordCommandValidator()
        {
            RuleFor(c => c.NewPassword).NotEmpty().MinimumLength(8);
        }
    }
}
