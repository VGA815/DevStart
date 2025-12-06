using FluentValidation;

namespace DevStart.Application.UserPreferences.Delete
{
    internal sealed class DeleteUserPreferenceCommandValidator : AbstractValidator<DeleteUserPreferenceCommand>
    {
        public DeleteUserPreferenceCommandValidator()
        {
            RuleFor(up => up.UserId).NotEmpty();
        }
    }
}
