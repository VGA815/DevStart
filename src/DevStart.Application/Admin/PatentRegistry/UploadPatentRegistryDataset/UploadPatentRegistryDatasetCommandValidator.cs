using FluentValidation;

namespace DevStart.Application.Admin.PatentRegistry.UploadPatentRegistryDataset
{
    internal sealed class UploadPatentRegistryDatasetCommandValidator
        : AbstractValidator<UploadPatentRegistryDatasetCommand>
    {
        public UploadPatentRegistryDatasetCommandValidator()
        {
            RuleFor(c => c.Content).NotNull();

            RuleFor(c => c.Length)
                .GreaterThan(0)
                .WithMessage("Файл пуст.")
                .LessThanOrEqualTo(UploadPatentRegistryDatasetCommand.MaxLengthBytes)
                .WithMessage(
                    $"Выгрузка должна быть не больше {UploadPatentRegistryDatasetCommand.MaxLengthBytes / (1024 * 1024)} МБ. "
                    + "Полный реестр загружается квартальным джобом по настроенному URL.");

            RuleFor(c => c.Kind)
                .IsInEnum()
                .WithMessage("Укажите вид объекта: файл открытых данных сам о нём не сообщает.");

            RuleFor(c => c.FileName).NotEmpty().MaximumLength(260);
        }
    }
}
