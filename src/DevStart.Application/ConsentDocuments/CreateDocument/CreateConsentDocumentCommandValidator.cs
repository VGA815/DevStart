using FluentValidation;

namespace DevStart.Application.ConsentDocuments.CreateDocument
{
    internal sealed class CreateConsentDocumentCommandValidator : AbstractValidator<CreateConsentDocumentCommand>
    {
        public CreateConsentDocumentCommandValidator()
        {
            RuleFor(c => c.Version).NotEmpty().MaximumLength(20);
            RuleFor(c => c.Title).NotEmpty().MaximumLength(255);
            RuleFor(c => c.Content).NotEmpty();
        }
    }
}
