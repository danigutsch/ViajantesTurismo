using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Common.UnitTests.Results;

public class ResultConvertErrorNonGenericToGenericTests
{
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

        // Act
        // Assert
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
}
