using FluentValidation;

namespace DevStart.Application.Profiles.Delete
{
    internal sealed class DeleteProfileCommandValidator : AbstractValidator<DeleteProfileCommand>
    {
        public DeleteProfileCommandValidator()
        {
            RuleFor(p => p.UserId).NotEmpty();
        }
    }
}
