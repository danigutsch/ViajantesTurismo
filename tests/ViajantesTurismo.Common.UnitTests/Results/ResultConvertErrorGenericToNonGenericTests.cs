using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Common.UnitTests.Results;

public class ResultConvertErrorGenericToNonGenericTests
{
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

        // Act
        // Assert
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
