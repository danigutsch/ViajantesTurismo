using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Common.UnitTests.Results;

public class ResultExtensionsTests
{
    [Fact]
    public void ConvertError_WithFailedResult_ReturnsFailedResultWithSameErrors()
    {
        // Arrange
        var sourceResult = Result<string>.Invalid("Original error", "field", "message");

        // Act
        var convertedResult = sourceResult.ConvertError<string, int>();

        // Assert
        Assert.False(convertedResult.IsSuccess);
        Assert.True(convertedResult.IsFailure);
        Assert.Equal(ResultStatus.Invalid, convertedResult.Status);
        Assert.NotNull(convertedResult.ErrorDetails);
        Assert.Equal("Original error", convertedResult.ErrorDetails.Detail);
        Assert.NotNull(convertedResult.ErrorDetails.ValidationErrors);
        Assert.Single(convertedResult.ErrorDetails.ValidationErrors);
        Assert.Equal(["message"], convertedResult.ErrorDetails.ValidationErrors["field"]);
    }

    [Fact]
    public void ConvertError_WithNotFoundResult_PreservesNotFoundStatus()
    {
        // Arrange
        var sourceResult = Result<string>.NotFound("Resource not found");

        // Act
        var convertedResult = sourceResult.ConvertError<string, int>();

        // Assert
        Assert.False(convertedResult.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, convertedResult.Status);
        Assert.NotNull(convertedResult.ErrorDetails);
        Assert.Equal("Resource not found", convertedResult.ErrorDetails.Detail);
    }

    [Fact]
    public void ConvertError_WithConflictResult_PreservesConflictStatus()
    {
        // Arrange
        var sourceResult = Result<string>.Conflict("Conflict occurred");

        // Act
        var convertedResult = sourceResult.ConvertError<string, int>();

        // Assert
        Assert.False(convertedResult.IsSuccess);
        Assert.Equal(ResultStatus.Conflict, convertedResult.Status);
        Assert.NotNull(convertedResult.ErrorDetails);
        Assert.Equal("Conflict occurred", convertedResult.ErrorDetails.Detail);
    }

    [Fact]
    public void ConvertError_WithUnauthorizedResult_PreservesUnauthorizedStatus()
    {
        // Arrange
        var sourceResult = Result<string>.Unauthorized("Unauthorized access");

        // Act
        var convertedResult = sourceResult.ConvertError<string, int>();

        // Assert
        Assert.False(convertedResult.IsSuccess);
        Assert.Equal(ResultStatus.Unauthorized, convertedResult.Status);
        Assert.NotNull(convertedResult.ErrorDetails);
        Assert.Equal("Unauthorized access", convertedResult.ErrorDetails.Detail);
    }

    [Fact]
    public void ConvertError_WithForbiddenResult_PreservesForbiddenStatus()
    {
        // Arrange
        var sourceResult = Result<string>.Forbidden("Access forbidden");

        // Act
        var convertedResult = sourceResult.ConvertError<string, int>();

        // Assert
        Assert.False(convertedResult.IsSuccess);
        Assert.Equal(ResultStatus.Forbidden, convertedResult.Status);
        Assert.NotNull(convertedResult.ErrorDetails);
        Assert.Equal("Access forbidden", convertedResult.ErrorDetails.Detail);
    }

    [Fact]
    public void ConvertError_WithErrorResult_PreservesErrorStatus()
    {
        // Arrange
        var sourceResult = Result<string>.Error("Internal error");

        // Act
        var convertedResult = sourceResult.ConvertError<string, int>();

        // Assert
        Assert.False(convertedResult.IsSuccess);
        Assert.Equal(ResultStatus.Error, convertedResult.Status);
        Assert.NotNull(convertedResult.ErrorDetails);
        Assert.Equal("Internal error", convertedResult.ErrorDetails.Detail);
    }

    [Fact]
    public void ConvertError_WithCriticalErrorResult_PreservesCriticalErrorStatus()
    {
        // Arrange
        var sourceResult = Result<string>.CriticalError("Critical failure");

        // Act
        var convertedResult = sourceResult.ConvertError<string, int>();

        // Assert
        Assert.False(convertedResult.IsSuccess);
        Assert.Equal(ResultStatus.CriticalError, convertedResult.Status);
        Assert.NotNull(convertedResult.ErrorDetails);
        Assert.Equal("Critical failure", convertedResult.ErrorDetails.Detail);
    }

    [Fact]
    public void ConvertError_WithUnavailableResult_PreservesUnavailableStatus()
    {
        // Arrange
        var sourceResult = Result<string>.Unavailable("Service unavailable");

        // Act
        var convertedResult = sourceResult.ConvertError<string, int>();

        // Assert
        Assert.False(convertedResult.IsSuccess);
        Assert.Equal(ResultStatus.Unavailable, convertedResult.Status);
        Assert.NotNull(convertedResult.ErrorDetails);
        Assert.Equal("Service unavailable", convertedResult.ErrorDetails.Detail);
    }

    [Fact]
    public void ConvertError_WithSuccessfulResult_ThrowsInvalidOperationException()
    {
        // Arrange
        var sourceResult = Result<string>.Ok("Success");

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            sourceResult.ConvertError<string, int>());

        Assert.Equal("Cannot convert a successful result. Only failed results can be converted.", exception.Message);
    }

    [Fact]
    public void ConvertError_WithMultipleValidationErrors_PreservesAllErrors()
    {
        // Arrange
        var errors = new ValidationErrors();
        errors.Add(Result<string>.Invalid("Error 1", "field1", "message1"));
        errors.Add(Result<string>.Invalid("Error 2", "field2", "message2"));
        errors.Add(Result<string>.Invalid("Error 3", "field3", "message3"));
        var sourceResult = errors.ToResult<string>();

        // Act
        var convertedResult = sourceResult.ConvertError<string, int>();

        // Assert
        Assert.False(convertedResult.IsSuccess);
        Assert.Equal(ResultStatus.Invalid, convertedResult.Status);
        Assert.NotNull(convertedResult.ErrorDetails);
        Assert.NotNull(convertedResult.ErrorDetails.ValidationErrors);
        Assert.Equal(3, convertedResult.ErrorDetails.ValidationErrors.Count);
        Assert.Contains("field1", convertedResult.ErrorDetails.ValidationErrors.Keys);
        Assert.Contains("field2", convertedResult.ErrorDetails.ValidationErrors.Keys);
        Assert.Contains("field3", convertedResult.ErrorDetails.ValidationErrors.Keys);
    }

    [Fact]
    public void ConvertError_BetweenDifferentTypes_ConvertsSuccessfully()
    {
        // Arrange - source is string, target is custom type
        var sourceResult = Result<string>.NotFound("Entity not found");

        // Act
        var convertedResult = sourceResult.ConvertError<string, List<int>>();

        // Assert
        Assert.False(convertedResult.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, convertedResult.Status);
        Assert.NotNull(convertedResult.ErrorDetails);
        Assert.Equal("Entity not found", convertedResult.ErrorDetails.Detail);
    }

    [Fact]
    public void ConvertError_FromValueTypeToReferenceType_ConvertsSuccessfully()
    {
        // Arrange
        var sourceResult = Result<int>.Invalid("Invalid number", "number", "Must be positive");

        // Act
        var convertedResult = sourceResult.ConvertError<int, string>();

        // Assert
        Assert.False(convertedResult.IsSuccess);
        Assert.Equal(ResultStatus.Invalid, convertedResult.Status);
        Assert.NotNull(convertedResult.ErrorDetails);
        Assert.Equal("Invalid number", convertedResult.ErrorDetails.Detail);
        Assert.NotNull(convertedResult.ErrorDetails.ValidationErrors);
        Assert.Contains("number", convertedResult.ErrorDetails.ValidationErrors.Keys);
        Assert.Equal(["Must be positive"], convertedResult.ErrorDetails.ValidationErrors["number"]);
    }

    [Fact]
    public void ConvertError_FromReferenceTypeToValueType_ConvertsSuccessfully()
    {
        // Arrange
        var sourceResult = Result<string>.Conflict("Duplicate entry");

        // Act
        var convertedResult = sourceResult.ConvertError<string, decimal>();

        // Assert
        Assert.False(convertedResult.IsSuccess);
        Assert.Equal(ResultStatus.Conflict, convertedResult.Status);
        Assert.NotNull(convertedResult.ErrorDetails);
        Assert.Equal("Duplicate entry", convertedResult.ErrorDetails.Detail);
    }

    [Fact]
    public void ConvertError_ChainedConversion_PreservesOriginalErrors()
    {
        // Arrange
        var originalResult = Result<string>.Invalid("Original error", "field", "message");

        // Act - convert through multiple types
        var firstConversion = originalResult.ConvertError<string, int>();
        var secondConversion = firstConversion.ConvertError<int, decimal>();
        var thirdConversion = secondConversion.ConvertError<decimal, bool>();

        // Assert - errors should be preserved through the chain
        Assert.False(thirdConversion.IsSuccess);
        Assert.Equal(ResultStatus.Invalid, thirdConversion.Status);
        Assert.NotNull(thirdConversion.ErrorDetails);
        Assert.Equal("Original error", thirdConversion.ErrorDetails.Detail);
        Assert.NotNull(thirdConversion.ErrorDetails.ValidationErrors);
        Assert.Contains("field", thirdConversion.ErrorDetails.ValidationErrors.Keys);
        Assert.Equal(["message"], thirdConversion.ErrorDetails.ValidationErrors["field"]);
    }
}