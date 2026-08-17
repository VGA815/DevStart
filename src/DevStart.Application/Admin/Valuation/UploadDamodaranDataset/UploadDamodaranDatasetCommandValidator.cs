using FluentValidation;

namespace DevStart.Application.Admin.Valuation.UploadDamodaranDataset
{
    internal sealed class UploadDamodaranDatasetCommandValidator
        : AbstractValidator<UploadDamodaranDatasetCommand>
    {
        public UploadDamodaranDatasetCommandValidator()
        {
            RuleFor(c => c.Content).NotNull();

            // Rejected on the stated length before the body is buffered, so an oversized upload costs
            // a validation failure rather than the memory it claims to need.
            RuleFor(c => c.Length)
                .GreaterThan(0)
                .WithMessage("The uploaded file is empty.")
                .LessThanOrEqualTo(UploadDamodaranDatasetCommand.MaxLengthBytes)
                .WithMessage(
                    $"The dataset must be at most {UploadDamodaranDatasetCommand.MaxLengthBytes / (1024 * 1024)} MB. "
                    + "Export just the industry sheet to CSV.");

            RuleFor(c => c.DatasetYear)
                .InclusiveBetween(2000, 2100)
                .WithMessage("State the year of the Damodaran release (it is not read from the file).");

            // A message attaches to the validator immediately before it, so each failure mode carries
            // its own — otherwise the helpful text only ever shows for the length case.
            RuleFor(c => c.DatasetRegion)
                .NotEmpty()
                .WithMessage("State the regional slice of the dataset, e.g. \"Emerging Markets\".")
                .MaximumLength(64)
                .WithMessage("The regional slice must be at most 64 characters.");

            RuleFor(c => c.FileName).NotEmpty().MaximumLength(260);
        }
    }
}
