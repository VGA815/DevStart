using System.Linq.Expressions;
using DevStart.Domain.Messages;
using FluentValidation;

namespace DevStart.Application.Messages.Create
{
    internal sealed class CreateMessageCommandValidator : AbstractValidator<CreateMessageCommand>
    {
        public CreateMessageCommandValidator()
        {
            RuleFor(m => m.ReceiverId).NotEmpty();
            RuleFor(m => m.ReceiverType).IsInEnum();
            RuleFor(m => m.TextContent).MaximumLength(MessageRules.MaxTextLength);

            AttachmentRule(m => m.MediaIds);
            AttachmentRule(m => m.MetricIds);
            AttachmentRule(m => m.DocumentIds);
            AttachmentRule(m => m.FileIds);
        }

        private void AttachmentRule(Expression<Func<CreateMessageCommand, List<Guid>?>> selector) =>
            RuleFor(selector)
                .Must(ids => ids is null || ids.Count <= MessageRules.MaxAttachmentsPerKind)
                .WithMessage($"No more than {MessageRules.MaxAttachmentsPerKind} attachments of one kind are allowed.");
    }
}
