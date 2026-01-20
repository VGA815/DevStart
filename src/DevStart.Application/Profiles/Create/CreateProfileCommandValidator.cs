using FluentValidation;

namespace DevStart.Application.Profiles.Create
{
    internal class CreateProfileCommandValidator : AbstractValidator<CreateProfileCommand>
    {
        public CreateProfileCommandValidator()
        {
            RuleFor(c => c.UserId).NotEmpty();
            RuleFor(c => c.IsPublic).NotEmpty();
        }
    }
}
