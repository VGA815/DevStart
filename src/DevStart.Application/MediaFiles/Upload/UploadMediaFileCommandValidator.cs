using FluentValidation;

namespace DevStart.Application.MediaFiles.Upload
{
    internal sealed class UploadMediaFileCommandValidator : AbstractValidator<UploadMediaFileCommand>
    {
        public UploadMediaFileCommandValidator()
        {
            RuleFor(x => x.OwnerId).NotEmpty();
            RuleFor(x => x.Bucket).NotEmpty();
        }
    }
}
