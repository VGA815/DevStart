using FluentValidation;

namespace DevStart.Application.UserPreferences.Create
{
    internal sealed class CreateUserPreferenceCommandValidator : AbstractValidator<CreateUserPreferenceCommand>
    {
        public CreateUserPreferenceCommandValidator()
        {
            RuleFor(up => up.UserId).NotEmpty();
            RuleFor(up => up.ReceiveNotifications).NotEmpty();
            RuleFor(up => up.Theme).IsInEnum();
        }
    }
}
