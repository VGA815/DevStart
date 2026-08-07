using FluentValidation;

namespace DevStart.Application.ChatFiles.Upload
{
    internal sealed class UploadChatFileCommandValidator : AbstractValidator<UploadChatFileCommand>
    {
        public UploadChatFileCommandValidator()
        {
            RuleFor(f => f.FileName).NotEmpty();
            RuleFor(f => f.ContentType).NotEmpty();
            RuleFor(f => f.FileStream).NotNull();
        }
    }
}
