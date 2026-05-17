using DevStart.Application;
using DevStart.Application.MediaFiles.Upload;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace DevStart.UnitTests.Application.Validation;

public sealed class UploadMediaFileCommandValidatorTests
{
    private readonly IValidator<UploadMediaFileCommand> _validator = new ServiceCollection()
        .AddApplication()
        .BuildServiceProvider()
        .GetRequiredService<IValidator<UploadMediaFileCommand>>();

    [Fact]
    public void Validate_ShouldPass_ForValidUpload()
    {
        var result = _validator.Validate(new UploadMediaFileCommand(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            new MemoryStream([1, 2, 3]),
            "image/png",
            3,
            "media"));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_ForEmptyOwnerAndBucket()
    {
        var result = _validator.Validate(new UploadMediaFileCommand(
            Guid.Empty,
            new MemoryStream([1, 2, 3]),
            "image/png",
            3,
            string.Empty));

        result.IsValid.ShouldBeFalse();
        result.Errors.Select(error => error.PropertyName).ShouldContain("OwnerId");
        result.Errors.Select(error => error.PropertyName).ShouldContain("Bucket");
    }
}
