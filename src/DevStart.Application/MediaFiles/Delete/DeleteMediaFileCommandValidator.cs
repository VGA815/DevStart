using FluentValidation;

namespace DevStart.Application.MediaFiles.Delete
{
    internal sealed class DeleteMediaFileCommandValidator : AbstractValidator<DeleteMediaFileCommand>
    {
        public DeleteMediaFileCommandValidator()
        {
            RuleFor(x => x.FileId).NotEmpty();
        }
    }
}
