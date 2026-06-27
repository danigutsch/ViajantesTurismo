using SharedKernel.Results;
using static ViajantesTurismo.Common.UnitTests.Results.ResultConvertErrorMalformedStatusTestsHelpers;

namespace ViajantesTurismo.Common.UnitTests.Results;

public class ResultConvertErrorMalformedStatusTests
{
    private const string CannotConvertSuccessfulResultMessage =
        "Cannot convert a successful result. Only failed results can be converted.";

    [Theory]
    [InlineData((int)ResultStatus.Created, CannotConvertSuccessfulResultMessage)]
    [InlineData((int)ResultStatus.Unknown, "Unsupported result status: Unknown")]
    [InlineData(999, "Unsupported result status: 999")]
    public void ConvertError_nongenerictogeneric_malformed_status_throws_expected_exception(
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
    public void ConvertError_generictonongeneric_malformed_status_throws_expected_exception(
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
    public void ConvertError_generictogeneric_malformed_status_throws_expected_exception(
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

}
