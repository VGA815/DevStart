using FluentValidation;

namespace DevStart.Application.UserPreferences.Update
{
    internal sealed class UpdateUserPreferenceCommandValidator : AbstractValidator<UpdateUserPreferenceCommand>
    {
        public UpdateUserPreferenceCommandValidator()
        {
            RuleFor(up => up.UserId).NotEmpty();
            RuleFor(up => up.ReceiveNotifications).NotEmpty();
            RuleFor(up => up.Theme).IsInEnum();
        }
    }
}
