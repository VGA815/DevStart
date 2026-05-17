using DevStart.SharedKernel;
using Shouldly;

namespace DevStart.UnitTests.SharedKernel;

public sealed class ResultTests
{
    [Fact]
    public void Success_ShouldCreateSuccessfulResult()
    {
        Result result = Result.Success();

        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
        result.Error.ShouldBe(Error.None);
    }

    [Fact]
    public void Failure_ShouldCreateFailureResult()
    {
        Error error = Error.Problem("Test.Error", "Test error");

        Result result = Result.Failure(error);

        result.IsSuccess.ShouldBeFalse();
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(error);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Constructor_ShouldRejectInvalidSuccessAndErrorCombinations(bool isSuccess)
    {
        Error error = isSuccess ? Error.Problem("Test.Error", "Test error") : Error.None;

        Should.Throw<ArgumentException>(() => new Result(isSuccess, error))
            .ParamName.ShouldBe("error");
    }

    [Fact]
    public void Value_ShouldThrow_WhenResultIsFailure()
    {
        Result<string> result = Result.Failure<string>(Error.Problem("Test.Error", "Test error"));

        Should.Throw<InvalidOperationException>(() => result.Value)
            .Message.ShouldBe("The value of a failure result can't be accessed.");
    }

    [Fact]
    public void ImplicitConversion_ShouldCreateSuccess_WhenValueIsNotNull()
    {
        Result<string> result = "value";

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("value");
    }

    [Fact]
    public void ImplicitConversion_ShouldCreateFailure_WhenValueIsNull()
    {
        string? value = null;

        Result<string> result = value;

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(Error.NullValue);
    }
}
