using System.Reflection;

namespace SharedKernel.Results.Tests;

[Trait(TestTraits.CapabilityName, TestTraits.ResultCapability)]
[Trait(TestTraits.CategoryName, TestTraits.CompositionCategory)]
public sealed class ResultConvertErrorTests
{
    [Fact]
    public void Converts_A_Non_Generic_Failure_To_A_Generic_Failure()
    {
        // Arrange
        var source = Result.NotFound("Tour not found");

        // Act
        var converted = source.ConvertError<string>();

        // Assert
        Assert.Equal(ResultStatus.NotFound, converted.Status);
        Assert.Equal("Tour not found", converted.ErrorDetails!.Detail);
    }

    [Fact]
    public void Converts_A_Generic_Failure_To_A_Non_Generic_Failure()
    {
        // Arrange
        var source = Result.Conflict<string>("Tour already exists");

        // Act
        var converted = source.ConvertError();

        // Assert
        Assert.Equal(ResultStatus.Conflict, converted.Status);
        Assert.Equal("Tour already exists", converted.ErrorDetails!.Detail);
    }

    [Fact]
    public void Converts_A_Generic_Failure_To_Another_Generic_Failure()
    {
        // Arrange
        var source = Result.Error<string>("Unexpected failure");

        // Act
        var converted = source.ConvertError<string, int>();

        // Assert
        Assert.Equal(ResultStatus.Error, converted.Status);
        Assert.Equal("Unexpected failure", converted.ErrorDetails!.Detail);
    }

    [Fact]
    public void Preserves_Validation_Errors_When_Converting_Invalid_Results()
    {
        // Arrange
        var source = Result.Invalid("Validation failed", "Name", "Name is required");

        // Act
        var converted = source.ConvertError<string>();

        // Assert
        Assert.Equal(ResultStatus.Invalid, converted.Status);
        Assert.Equal(["Name is required"], converted.ErrorDetails!.ValidationErrors!["Name"]);
    }

    [Theory]
    [InlineData((int)ResultStatus.Created, "Cannot convert a successful result. Only failed results can be converted.")]
    [InlineData((int)ResultStatus.Unknown, "Unsupported result status: Unknown")]
    [InlineData(999, "Unsupported result status: 999")]
    public void Throws_Expected_Exception_For_Malformed_Non_Generic_Status(int statusValue, string expectedMessage)
    {
        // Arrange
        var malformedResult = CreateMalformedResult((ResultStatus)statusValue, new ResultError("Malformed result status."));

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => malformedResult.ConvertError<string>());

        // Assert
        Assert.Equal(expectedMessage, exception.Message);
    }

    [Theory]
    [InlineData((int)ResultStatus.NoContent, "Cannot convert a successful result. Only failed results can be converted.")]
    [InlineData((int)ResultStatus.Unknown, "Unsupported result status: Unknown")]
    [InlineData(999, "Unsupported result status: 999")]
    public void Throws_Expected_Exception_For_Malformed_Generic_Status(int statusValue, string expectedMessage)
    {
        // Arrange
        var malformedResult = CreateMalformedGenericResult((ResultStatus)statusValue, "payload", new ResultError("Malformed result status."));

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => malformedResult.ConvertError<string, int>());

        // Assert
        Assert.Equal(expectedMessage, exception.Message);
    }

    private static Result CreateMalformedResult(ResultStatus status, ResultError? error)
    {
        var constructor = typeof(Result).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types: [typeof(ResultStatus), typeof(ResultError)],
            modifiers: null);

        Assert.NotNull(constructor);
        return (Result)constructor.Invoke([status, error]);
    }

    private static Result<T> CreateMalformedGenericResult<T>(ResultStatus status, T value, ResultError? error)
        where T : notnull
    {
        var constructor = typeof(Result<T>).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types: [typeof(ResultStatus), typeof(T), typeof(ResultError)],
            modifiers: null);

        Assert.NotNull(constructor);
        return (Result<T>)constructor.Invoke([status, value, error]);
    }
}
