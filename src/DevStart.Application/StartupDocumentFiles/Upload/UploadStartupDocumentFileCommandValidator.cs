using FluentValidation;

namespace DevStart.Application.StartupDocumentFiles.Upload
{
    internal sealed class UploadStartupDocumentFileCommandValidator : AbstractValidator<UploadStartupDocumentFileCommand>
    {
        public UploadStartupDocumentFileCommandValidator()
        {
            RuleFor(sd => sd.StartupId).NotEmpty();
            RuleFor(sd => sd.FileSize).NotEmpty();
            RuleFor(sd => sd.Bucket).NotEmpty();
            RuleFor(sd => sd.DocumentName).NotEmpty();
            RuleFor(sd => sd.ContentType).NotEmpty();
        }
    }
}
