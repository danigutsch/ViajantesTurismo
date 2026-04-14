using System.Reflection;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Common.UnitTests.Results;

public class ResultConvertErrorMalformedStatusTests
{
    private const string CannotConvertSuccessfulResultMessage =
        "Cannot convert a successful result. Only failed results can be converted.";

    [Theory]
    [InlineData((int)ResultStatus.Created, CannotConvertSuccessfulResultMessage)]
    [InlineData((int)ResultStatus.Unknown, "Unsupported result status: Unknown")]
    [InlineData(999, "Unsupported result status: 999")]
    public void ConvertError_NonGenericToGeneric_Malformed_Status_Throws_Expected_Exception(
        int statusValue,
        string expectedMessage)
    {
        // Arrange
        var malformedSourceResult = CreateMalformedResult(
            (ResultStatus)statusValue,
            new ResultError("Malformed result status."));

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => malformedSourceResult.ConvertError<string>());

        // Assert
        Assert.Equal(expectedMessage, exception.Message);
    }

    [Theory]
    [InlineData((int)ResultStatus.NoContent, CannotConvertSuccessfulResultMessage)]
    [InlineData((int)ResultStatus.Unknown, "Unsupported result status: Unknown")]
    [InlineData(999, "Unsupported result status: 999")]
    public void ConvertError_GenericToNonGeneric_Malformed_Status_Throws_Expected_Exception(
        int statusValue,
        string expectedMessage)
    {
        // Arrange
        var malformedSourceResult = CreateMalformedGenericResult(
            (ResultStatus)statusValue,
            "payload",
            new ResultError("Malformed result status."));

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => malformedSourceResult.ConvertError());

        // Assert
        Assert.Equal(expectedMessage, exception.Message);
    }

    [Theory]
    [InlineData((int)ResultStatus.NoContent, CannotConvertSuccessfulResultMessage)]
    [InlineData((int)ResultStatus.Unknown, "Unsupported result status: Unknown")]
    [InlineData(999, "Unsupported result status: 999")]
    public void ConvertError_GenericToGeneric_Malformed_Status_Throws_Expected_Exception(
        int statusValue,
        string expectedMessage)
    {
        // Arrange
        var malformedSourceResult = CreateMalformedGenericResult(
            (ResultStatus)statusValue,
            "payload",
            new ResultError("Malformed result status."));

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => malformedSourceResult.ConvertError<string, int>());

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
