using SharedKernel.Results;

namespace ViajantesTurismo.Common.UnitTests.Results;

public class ResultConvertErrorGenericToGenericTests
{
    [Fact]
    public void Convert_error_with_failed_result_returns_failed_result_with_same_errors()
    {
        // Arrange
        var sourceResult = Result.Invalid<string>("Original error", "field", "message");

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
    public void Convert_error_with_not_found_result_preserves_not_found_status()
    {
        // Arrange
        var sourceResult = Result.NotFound<string>("Resource not found");

        // Act
        var convertedResult = sourceResult.ConvertError<string, int>();

        // Assert
        Assert.False(convertedResult.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, convertedResult.Status);
        Assert.NotNull(convertedResult.ErrorDetails);
        Assert.Equal("Resource not found", convertedResult.ErrorDetails.Detail);
    }

    [Fact]
    public void Convert_error_with_conflict_result_preserves_conflict_status()
    {
        // Arrange
        var sourceResult = Result.Conflict<string>("Conflict occurred");

        // Act
        var convertedResult = sourceResult.ConvertError<string, int>();

        // Assert
        Assert.False(convertedResult.IsSuccess);
        Assert.Equal(ResultStatus.Conflict, convertedResult.Status);
        Assert.NotNull(convertedResult.ErrorDetails);
        Assert.Equal("Conflict occurred", convertedResult.ErrorDetails.Detail);
    }

    [Fact]
    public void Convert_error_with_unauthorized_result_preserves_unauthorized_status()
    {
        // Arrange
        var sourceResult = Result.Unauthorized<string>("Unauthorized access");

        // Act
        var convertedResult = sourceResult.ConvertError<string, int>();

        // Assert
        Assert.False(convertedResult.IsSuccess);
        Assert.Equal(ResultStatus.Unauthorized, convertedResult.Status);
        Assert.NotNull(convertedResult.ErrorDetails);
        Assert.Equal("Unauthorized access", convertedResult.ErrorDetails.Detail);
    }

    [Fact]
    public void Convert_error_with_forbidden_result_preserves_forbidden_status()
    {
        // Arrange
        var sourceResult = Result.Forbidden<string>("Access forbidden");

        // Act
        var convertedResult = sourceResult.ConvertError<string, int>();

        // Assert
        Assert.False(convertedResult.IsSuccess);
        Assert.Equal(ResultStatus.Forbidden, convertedResult.Status);
        Assert.NotNull(convertedResult.ErrorDetails);
        Assert.Equal("Access forbidden", convertedResult.ErrorDetails.Detail);
    }

    [Fact]
    public void Convert_error_with_error_result_preserves_error_status()
    {
        // Arrange
        var sourceResult = Result.Error<string>("Internal error");

        // Act
        var convertedResult = sourceResult.ConvertError<string, int>();

        // Assert
        Assert.False(convertedResult.IsSuccess);
        Assert.Equal(ResultStatus.Error, convertedResult.Status);
        Assert.NotNull(convertedResult.ErrorDetails);
        Assert.Equal("Internal error", convertedResult.ErrorDetails.Detail);
    }

    [Fact]
    public void Convert_error_with_critical_error_result_preserves_critical_error_status()
    {
        // Arrange
        var sourceResult = Result.CriticalError<string>("Critical failure");

        // Act
        var convertedResult = sourceResult.ConvertError<string, int>();

        // Assert
        Assert.False(convertedResult.IsSuccess);
        Assert.Equal(ResultStatus.CriticalError, convertedResult.Status);
        Assert.NotNull(convertedResult.ErrorDetails);
        Assert.Equal("Critical failure", convertedResult.ErrorDetails.Detail);
    }

    [Fact]
    public void Convert_error_with_unavailable_result_preserves_unavailable_status()
    {
        // Arrange
        var sourceResult = Result.Unavailable<string>("Service unavailable");

        // Act
        var convertedResult = sourceResult.ConvertError<string, int>();

        // Assert
        Assert.False(convertedResult.IsSuccess);
        Assert.Equal(ResultStatus.Unavailable, convertedResult.Status);
        Assert.NotNull(convertedResult.ErrorDetails);
        Assert.Equal("Service unavailable", convertedResult.ErrorDetails.Detail);
    }

    [Fact]
    public void Convert_error_with_successful_result_throws_invalid_operation_exception()
    {
        // Arrange
        var sourceResult = Result.Ok("Success");

        // Act
        // Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            sourceResult.ConvertError<string, int>());

        Assert.Equal("Cannot convert a successful result. Only failed results can be converted.", exception.Message);
    }

    [Fact]
    public void Convert_error_with_multiple_validation_errors_preserves_all_errors()
    {
        // Arrange
        var errors = new ValidationErrors();
        errors.Add(Result.Invalid<string>("Error 1", "field1", "message1"));
        errors.Add(Result.Invalid<string>("Error 2", "field2", "message2"));
        errors.Add(Result.Invalid<string>("Error 3", "field3", "message3"));
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
    public void Convert_error_between_different_types_converts_successfully()
    {
        // Arrange
        var sourceResult = Result.NotFound<string>("Entity not found");

        // Act
        var convertedResult = sourceResult.ConvertError<string, List<int>>();

        // Assert
        Assert.False(convertedResult.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, convertedResult.Status);
        Assert.NotNull(convertedResult.ErrorDetails);
        Assert.Equal("Entity not found", convertedResult.ErrorDetails.Detail);
    }

    [Fact]
    public void Convert_error_from_value_type_to_reference_type_converts_successfully()
    {
        // Arrange
        var sourceResult = Result.Invalid<int>("Invalid number", "number", "Must be positive");

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
    public void Convert_error_from_reference_type_to_value_type_converts_successfully()
    {
        // Arrange
        var sourceResult = Result.Conflict<string>("Duplicate entry");

        // Act
        var convertedResult = sourceResult.ConvertError<string, decimal>();

        // Assert
        Assert.False(convertedResult.IsSuccess);
        Assert.Equal(ResultStatus.Conflict, convertedResult.Status);
        Assert.NotNull(convertedResult.ErrorDetails);
        Assert.Equal("Duplicate entry", convertedResult.ErrorDetails.Detail);
    }

    [Fact]
    public void Convert_error_chained_conversion_preserves_original_errors()
    {
        // Arrange
        var originalResult = Result.Invalid<string>("Original error", "field", "message");

        // Act
        var firstConversion = originalResult.ConvertError<string, int>();
        var secondConversion = firstConversion.ConvertError<int, decimal>();
        var thirdConversion = secondConversion.ConvertError<decimal, bool>();

        // Assert
        Assert.False(thirdConversion.IsSuccess);
        Assert.Equal(ResultStatus.Invalid, thirdConversion.Status);
        Assert.NotNull(thirdConversion.ErrorDetails);
        Assert.Equal("Original error", thirdConversion.ErrorDetails.Detail);
        Assert.NotNull(thirdConversion.ErrorDetails.ValidationErrors);
        Assert.Contains("field", thirdConversion.ErrorDetails.ValidationErrors.Keys);
        Assert.Equal(["message"], thirdConversion.ErrorDetails.ValidationErrors["field"]);
    }
}
