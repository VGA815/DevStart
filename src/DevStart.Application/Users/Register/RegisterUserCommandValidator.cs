using FluentValidation;

namespace DevStart.Application.Users.Register
{
    internal sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
    {
        public RegisterUserCommandValidator()
        {
            RuleFor(c => c.Password).NotEmpty();
            RuleFor(c => c.Email).NotEmpty();
            RuleFor(c => c.Username).NotEmpty();
        }
    }
}
