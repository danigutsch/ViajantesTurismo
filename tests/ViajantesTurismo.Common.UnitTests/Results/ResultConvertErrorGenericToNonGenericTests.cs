using SharedKernel.Results;

namespace ViajantesTurismo.Common.UnitTests.Results;

public class ResultConvertErrorGenericToNonGenericTests
{
    [Fact]
    public void Generic_convertError_to_nonGeneric_with_invalid_result()
    {
        // Arrange
        var sourceResult = Result.Invalid<string>("Validation error", "field", "message");

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
    public void Generic_convertError_to_nonGeneric_with_notFound_result()
    {
        // Arrange
        var sourceResult = Result.NotFound<int>("Entity not found");

        // Act
        var convertedResult = sourceResult.ConvertError();

        // Assert
        Assert.False(convertedResult.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, convertedResult.Status);
        Assert.NotNull(convertedResult.ErrorDetails);
        Assert.Equal("Entity not found", convertedResult.ErrorDetails.Detail);
    }

    [Fact]
    public void Generic_convertError_to_nonGeneric_with_conflict_result()
    {
        // Arrange
        var sourceResult = Result.Conflict<decimal>("Duplicate key");

        // Act
        var convertedResult = sourceResult.ConvertError();

        // Assert
        Assert.False(convertedResult.IsSuccess);
        Assert.Equal(ResultStatus.Conflict, convertedResult.Status);
        Assert.NotNull(convertedResult.ErrorDetails);
        Assert.Equal("Duplicate key", convertedResult.ErrorDetails.Detail);
    }

    [Fact]
    public void Generic_convertError_to_nonGeneric_with_unauthorized_result()
    {
        // Arrange
        var sourceResult = Result.Unauthorized<bool>("Not authorized");

        // Act
        var convertedResult = sourceResult.ConvertError();

        // Assert
        Assert.False(convertedResult.IsSuccess);
        Assert.Equal(ResultStatus.Unauthorized, convertedResult.Status);
        Assert.NotNull(convertedResult.ErrorDetails);
        Assert.Equal("Not authorized", convertedResult.ErrorDetails.Detail);
    }

    [Fact]
    public void Generic_convertError_to_nonGeneric_with_forbidden_result()
    {
        // Arrange
        var sourceResult = Result.Forbidden<string>("Forbidden access");

        // Act
        var convertedResult = sourceResult.ConvertError();

        // Assert
        Assert.False(convertedResult.IsSuccess);
        Assert.Equal(ResultStatus.Forbidden, convertedResult.Status);
        Assert.NotNull(convertedResult.ErrorDetails);
        Assert.Equal("Forbidden access", convertedResult.ErrorDetails.Detail);
    }

    [Fact]
    public void Generic_convertError_to_nonGeneric_with_error_result()
    {
        // Arrange
        var sourceResult = Result.Error<List<int>>("Processing error");

        // Act
        var convertedResult = sourceResult.ConvertError();

        // Assert
        Assert.False(convertedResult.IsSuccess);
        Assert.Equal(ResultStatus.Error, convertedResult.Status);
        Assert.NotNull(convertedResult.ErrorDetails);
        Assert.Equal("Processing error", convertedResult.ErrorDetails.Detail);
    }

    [Fact]
    public void Generic_convertError_to_nonGeneric_with_criticalError_result()
    {
        // Arrange
        var sourceResult = Result.CriticalError<Dictionary<string, int>>("Critical failure");

        // Act
        var convertedResult = sourceResult.ConvertError();

        // Assert
        Assert.False(convertedResult.IsSuccess);
        Assert.Equal(ResultStatus.CriticalError, convertedResult.Status);
        Assert.NotNull(convertedResult.ErrorDetails);
        Assert.Equal("Critical failure", convertedResult.ErrorDetails.Detail);
    }

    [Fact]
    public void Generic_convertError_to_nonGeneric_with_unavailable_result()
    {
        // Arrange
        var sourceResult = Result.Unavailable<DateTime>("Service unavailable");

        // Act
        var convertedResult = sourceResult.ConvertError();

        // Assert
        Assert.False(convertedResult.IsSuccess);
        Assert.Equal(ResultStatus.Unavailable, convertedResult.Status);
        Assert.NotNull(convertedResult.ErrorDetails);
        Assert.Equal("Service unavailable", convertedResult.ErrorDetails.Detail);
    }

    [Fact]
    public void Generic_convertError_to_nonGeneric_with_successful_result_throws_invalidOperationException()
    {
        // Arrange
        var sourceResult = Result.Ok("Success");

        // Act
        // Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            sourceResult.ConvertError());

        Assert.Equal("Cannot convert a successful result. Only failed results can be converted.", exception.Message);
    }

    [Fact]
    public void Generic_convertError_to_nonGeneric_with_multiple_validationErrors_preserves_all()
    {
        // Arrange
        var errors = new ValidationErrors();
        errors.Add(Result.Invalid<int>("Error 1", "field1", "message1"));
        errors.Add(Result.Invalid<int>("Error 2", "field2", "message2"));
        errors.Add(Result.Invalid<int>("Error 3", "field3", "message3"));
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
