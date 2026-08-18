using DevStart.Domain.StartupPatents;
using DevStart.SharedKernel;
using FluentValidation;

namespace DevStart.Application.StartupPatents.Create
{
    internal sealed class CreateStartupPatentCommandValidator : AbstractValidator<CreateStartupPatentCommand>
    {
        public CreateStartupPatentCommandValidator(IDateTimeProvider dateTimeProvider)
        {
            int currentYear = dateTimeProvider.UtcNow.Year;

            RuleFor(x => x.StartupId).NotEmpty();
            RuleFor(x => x.Kind).IsInEnum();

            // The format is checked against the kind, so a mistyped number fails here — readably —
            // instead of resolving to "not found in the register", which says something else entirely.
            RuleFor(x => x.Number)
                .NotEmpty()
                .MaximumLength(100)
                .Must((command, number) => StartupPatent.IsNumberWellFormed(
                    command.Kind, StartupPatent.NormalizeNumber(number), currentYear))
                .WithMessage(command =>
                    $"Номер не соответствует виду объекта: ожидается {StartupPatent.NumberFormatHint(command.Kind)}.");
        }
    }
}
