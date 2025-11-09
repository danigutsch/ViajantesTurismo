using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Common.UnitTests.Results;

public class ResultExtensionsTests
{
    [Fact]
    public void Convert_Error_With_Failed_Result_Returns_Failed_Result_With_Same_Errors()
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
    public void Convert_Error_With_Not_Found_Result_Preserves_Not_Found_Status()
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
    public void Convert_Error_With_Conflict_Result_Preserves_Conflict_Status()
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
    public void Convert_Error_With_Unauthorized_Result_Preserves_Unauthorized_Status()
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
    public void Convert_Error_With_Forbidden_Result_Preserves_Forbidden_Status()
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
    public void Convert_Error_With_Error_Result_Preserves_Error_Status()
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
    public void Convert_Error_With_Critical_Error_Result_Preserves_Critical_Error_Status()
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
    public void Convert_Error_With_Unavailable_Result_Preserves_Unavailable_Status()
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
    public void Convert_Error_With_Successful_Result_Throws_Invalid_Operation_Exception()
    {
        // Arrange
        var sourceResult = Result<string>.Ok("Success");

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            sourceResult.ConvertError<string, int>());

        Assert.Equal("Cannot convert a successful result. Only failed results can be converted.", exception.Message);
    }

    [Fact]
    public void Convert_Error_With_Multiple_Validation_Errors_Preserves_All_Errors()
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
    public void Convert_Error_Between_Different_Types_Converts_Successfully()
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
    public void Convert_Error_From_Value_Type_To_Reference_Type_Converts_Successfully()
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
    public void Convert_Error_From_Reference_Type_To_Value_Type_Converts_Successfully()
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
    public void Convert_Error_Chained_Conversion_Preserves_Original_Errors()
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

    // Tests for Result.ConvertError<TTarget>() - non-generic to generic

    [Fact]
    public void NonGeneric_ConvertError_With_Invalid_Result_Converts_To_Generic()
    {
        // Arrange
        var sourceResult = Result.Invalid("Validation error", "field", "message");

        // Act
        var convertedResult = sourceResult.ConvertError<string>();

        // Assert
        Assert.False(convertedResult.IsSuccess);
        Assert.Equal(ResultStatus.Invalid, convertedResult.Status);
        Assert.NotNull(convertedResult.ErrorDetails);
        Assert.Equal("Validation error", convertedResult.ErrorDetails.Detail);
        Assert.NotNull(convertedResult.ErrorDetails.ValidationErrors);
        Assert.Contains("field", convertedResult.ErrorDetails.ValidationErrors.Keys);
        Assert.Equal(["message"], convertedResult.ErrorDetails.ValidationErrors["field"]);
    }

    [Fact]
    public void NonGeneric_ConvertError_With_NotFound_Result_Converts_To_Generic()
    {
        // Arrange
        var sourceResult = Result.NotFound("Resource not found");

        // Act
        var convertedResult = sourceResult.ConvertError<int>();

        // Assert
        Assert.False(convertedResult.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, convertedResult.Status);
        Assert.NotNull(convertedResult.ErrorDetails);
        Assert.Equal("Resource not found", convertedResult.ErrorDetails.Detail);
    }

    [Fact]
    public void NonGeneric_ConvertError_With_Conflict_Result_Converts_To_Generic()
    {
        // Arrange
        var sourceResult = Result.Conflict("Duplicate entry");

        // Act
        var convertedResult = sourceResult.ConvertError<decimal>();

        // Assert
        Assert.False(convertedResult.IsSuccess);
        Assert.Equal(ResultStatus.Conflict, convertedResult.Status);
        Assert.NotNull(convertedResult.ErrorDetails);
        Assert.Equal("Duplicate entry", convertedResult.ErrorDetails.Detail);
    }

    [Fact]
    public void NonGeneric_ConvertError_With_Unauthorized_Result_Converts_To_Generic()
    {
        // Arrange
        var sourceResult = Result.Unauthorized("Unauthorized");

        // Act
        var convertedResult = sourceResult.ConvertError<bool>();

        // Assert
        Assert.False(convertedResult.IsSuccess);
        Assert.Equal(ResultStatus.Unauthorized, convertedResult.Status);
        Assert.NotNull(convertedResult.ErrorDetails);
        Assert.Equal("Unauthorized", convertedResult.ErrorDetails.Detail);
    }

    [Fact]
    public void NonGeneric_ConvertError_With_Forbidden_Result_Converts_To_Generic()
    {
        // Arrange
        var sourceResult = Result.Forbidden("Access denied");

        // Act
        var convertedResult = sourceResult.ConvertError<string>();

        // Assert
        Assert.False(convertedResult.IsSuccess);
        Assert.Equal(ResultStatus.Forbidden, convertedResult.Status);
        Assert.NotNull(convertedResult.ErrorDetails);
        Assert.Equal("Access denied", convertedResult.ErrorDetails.Detail);
    }

    [Fact]
    public void NonGeneric_ConvertError_With_Error_Result_Converts_To_Generic()
    {
        // Arrange
        var sourceResult = Result.Error("Internal error");

        // Act
        var convertedResult = sourceResult.ConvertError<List<int>>();

        // Assert
        Assert.False(convertedResult.IsSuccess);
        Assert.Equal(ResultStatus.Error, convertedResult.Status);
        Assert.NotNull(convertedResult.ErrorDetails);
        Assert.Equal("Internal error", convertedResult.ErrorDetails.Detail);
    }

    [Fact]
    public void NonGeneric_ConvertError_With_CriticalError_Result_Converts_To_Generic()
    {
        // Arrange
        var sourceResult = Result.CriticalError("System failure");

        // Act
        var convertedResult = sourceResult.ConvertError<Dictionary<string, int>>();

        // Assert
        Assert.False(convertedResult.IsSuccess);
        Assert.Equal(ResultStatus.CriticalError, convertedResult.Status);
        Assert.NotNull(convertedResult.ErrorDetails);
        Assert.Equal("System failure", convertedResult.ErrorDetails.Detail);
    }

    [Fact]
    public void NonGeneric_ConvertError_With_Unavailable_Result_Converts_To_Generic()
    {
        // Arrange
        var sourceResult = Result.Unavailable("Service down");

        // Act
        var convertedResult = sourceResult.ConvertError<DateTime>();

        // Assert
        Assert.False(convertedResult.IsSuccess);
        Assert.Equal(ResultStatus.Unavailable, convertedResult.Status);
        Assert.NotNull(convertedResult.ErrorDetails);
        Assert.Equal("Service down", convertedResult.ErrorDetails.Detail);
    }

    [Fact]
    public void NonGeneric_ConvertError_With_Successful_Result_Throws_InvalidOperationException()
    {
        // Arrange
        var sourceResult = Result.Ok();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            sourceResult.ConvertError<string>());

        Assert.Equal("Cannot convert a successful result. Only failed results can be converted.", exception.Message);
    }

    [Fact]
    public void NonGeneric_ConvertError_With_Multiple_ValidationErrors_Preserves_All()
    {
        // Arrange
        var errors = new ValidationErrors();
        errors.Add(Result.Invalid("Error 1", "field1", "message1"));
        errors.Add(Result.Invalid("Error 2", "field2", "message2"));
        var sourceResult = errors.ToResult();

        // Act
        var convertedResult = sourceResult.ConvertError<int>();

        // Assert
        Assert.False(convertedResult.IsSuccess);
        Assert.Equal(ResultStatus.Invalid, convertedResult.Status);
        Assert.NotNull(convertedResult.ErrorDetails);
        Assert.NotNull(convertedResult.ErrorDetails.ValidationErrors);
        Assert.Equal(2, convertedResult.ErrorDetails.ValidationErrors.Count);
        Assert.Contains("field1", convertedResult.ErrorDetails.ValidationErrors.Keys);
        Assert.Contains("field2", convertedResult.ErrorDetails.ValidationErrors.Keys);
    }

    // Tests for Result<T>.ConvertError() - generic to non-generic

    [Fact]
    public void Generic_ConvertError_To_NonGeneric_With_Invalid_Result()
    {
        // Arrange
        var sourceResult = Result<string>.Invalid("Validation error", "field", "message");

        // Act
        var convertedResult = sourceResult.ConvertError();

        // Assert
        Assert.False(convertedResult.IsSuccess);
        Assert.Equal(ResultStatus.Invalid, convertedResult.Status);
        Assert.NotNull(convertedResult.ErrorDetails);
        Assert.Equal("Validation error", convertedResult.ErrorDetails.Detail);
        Assert.NotNull(convertedResult.ErrorDetails.ValidationErrors);
        Assert.Contains("field", convertedResult.ErrorDetails.ValidationErrors.Keys);
        Assert.Equal(["message"], convertedResult.ErrorDetails.ValidationErrors["field"]);
    }

    [Fact]
    public void Generic_ConvertError_To_NonGeneric_With_NotFound_Result()
    {
        // Arrange
        var sourceResult = Result<int>.NotFound("Entity not found");

        // Act
        var convertedResult = sourceResult.ConvertError();

        // Assert
        Assert.False(convertedResult.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, convertedResult.Status);
        Assert.NotNull(convertedResult.ErrorDetails);
        Assert.Equal("Entity not found", convertedResult.ErrorDetails.Detail);
    }

    [Fact]
    public void Generic_ConvertError_To_NonGeneric_With_Conflict_Result()
    {
        // Arrange
        var sourceResult = Result<decimal>.Conflict("Duplicate key");

        // Act
        var convertedResult = sourceResult.ConvertError();

        // Assert
        Assert.False(convertedResult.IsSuccess);
        Assert.Equal(ResultStatus.Conflict, convertedResult.Status);
        Assert.NotNull(convertedResult.ErrorDetails);
        Assert.Equal("Duplicate key", convertedResult.ErrorDetails.Detail);
    }

    [Fact]
    public void Generic_ConvertError_To_NonGeneric_With_Unauthorized_Result()
    {
        // Arrange
        var sourceResult = Result<bool>.Unauthorized("Not authorized");

        // Act
        var convertedResult = sourceResult.ConvertError();

        // Assert
        Assert.False(convertedResult.IsSuccess);
        Assert.Equal(ResultStatus.Unauthorized, convertedResult.Status);
        Assert.NotNull(convertedResult.ErrorDetails);
        Assert.Equal("Not authorized", convertedResult.ErrorDetails.Detail);
    }

    [Fact]
    public void Generic_ConvertError_To_NonGeneric_With_Forbidden_Result()
    {
        // Arrange
        var sourceResult = Result<string>.Forbidden("Forbidden access");

        // Act
        var convertedResult = sourceResult.ConvertError();

        // Assert
        Assert.False(convertedResult.IsSuccess);
        Assert.Equal(ResultStatus.Forbidden, convertedResult.Status);
        Assert.NotNull(convertedResult.ErrorDetails);
        Assert.Equal("Forbidden access", convertedResult.ErrorDetails.Detail);
    }

    [Fact]
    public void Generic_ConvertError_To_NonGeneric_With_Error_Result()
    {
        // Arrange
        var sourceResult = Result<List<int>>.Error("Processing error");

        // Act
        var convertedResult = sourceResult.ConvertError();

        // Assert
        Assert.False(convertedResult.IsSuccess);
        Assert.Equal(ResultStatus.Error, convertedResult.Status);
        Assert.NotNull(convertedResult.ErrorDetails);
        Assert.Equal("Processing error", convertedResult.ErrorDetails.Detail);
    }

    [Fact]
    public void Generic_ConvertError_To_NonGeneric_With_CriticalError_Result()
    {
        // Arrange
        var sourceResult = Result<Dictionary<string, int>>.CriticalError("Critical failure");

        // Act
        var convertedResult = sourceResult.ConvertError();

        // Assert
        Assert.False(convertedResult.IsSuccess);
        Assert.Equal(ResultStatus.CriticalError, convertedResult.Status);
        Assert.NotNull(convertedResult.ErrorDetails);
        Assert.Equal("Critical failure", convertedResult.ErrorDetails.Detail);
    }

    [Fact]
    public void Generic_ConvertError_To_NonGeneric_With_Unavailable_Result()
    {
        // Arrange
        var sourceResult = Result<DateTime>.Unavailable("Service unavailable");

        // Act
        var convertedResult = sourceResult.ConvertError();

        // Assert
        Assert.False(convertedResult.IsSuccess);
        Assert.Equal(ResultStatus.Unavailable, convertedResult.Status);
        Assert.NotNull(convertedResult.ErrorDetails);
        Assert.Equal("Service unavailable", convertedResult.ErrorDetails.Detail);
    }

    [Fact]
    public void Generic_ConvertError_To_NonGeneric_With_Successful_Result_Throws_InvalidOperationException()
    {
        // Arrange
        var sourceResult = Result<string>.Ok("Success");

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            sourceResult.ConvertError());

        Assert.Equal("Cannot convert a successful result. Only failed results can be converted.", exception.Message);
    }

    [Fact]
    public void Generic_ConvertError_To_NonGeneric_With_Multiple_ValidationErrors_Preserves_All()
    {
        // Arrange
        var errors = new ValidationErrors();
        errors.Add(Result<int>.Invalid("Error 1", "field1", "message1"));
        errors.Add(Result<int>.Invalid("Error 2", "field2", "message2"));
        errors.Add(Result<int>.Invalid("Error 3", "field3", "message3"));
        var sourceResult = errors.ToResult<int>();

        // Act
        var convertedResult = sourceResult.ConvertError();

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
}
