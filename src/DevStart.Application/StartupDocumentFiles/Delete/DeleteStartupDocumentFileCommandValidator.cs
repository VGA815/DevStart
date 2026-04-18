using FluentValidation;

namespace DevStart.Application.StartupDocumentFiles.Delete
{
    internal sealed class DeleteStartupDocumentFileCommandValidator : AbstractValidator<DeleteStartupDocumentFileCommand>
    {
        public DeleteStartupDocumentFileCommandValidator()
        {
            RuleFor(x => x.StartupDocumentFileId).NotEmpty();
        }
    }
}
