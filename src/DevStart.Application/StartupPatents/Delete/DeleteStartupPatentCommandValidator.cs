using FluentValidation;

namespace DevStart.Application.StartupPatents.Delete
{
    internal sealed class DeleteStartupPatentCommandValidator : AbstractValidator<DeleteStartupPatentCommand>
    {
        public DeleteStartupPatentCommandValidator()
        {
            RuleFor(x => x.PatentId).NotEmpty();
        }
    }
}
