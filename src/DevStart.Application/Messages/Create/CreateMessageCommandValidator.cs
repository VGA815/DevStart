using FluentValidation;

namespace DevStart.Application.Messages.Create
{
    internal sealed class CreateMessageCommandValidator : AbstractValidator<CreateMessageCommand>
    {
        public CreateMessageCommandValidator()
        {
            RuleFor(m => m.ReceiverId).NotEmpty();
        }
    }
}
