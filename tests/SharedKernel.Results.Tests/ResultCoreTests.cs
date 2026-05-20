namespace SharedKernel.Results.Tests;

[Trait(TestTraits.CapabilityName, TestTraits.ResultCapability)]
[Trait(TestTraits.CategoryName, TestTraits.CoreBehaviorCategory)]
public sealed class ResultCoreTests
{
    [Fact]
    public void Ok_creates_a_successful_result()
    {
        // Arrange
        // Act
        var result = Result.Ok();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Null(result.ErrorDetails);
    }

    [Fact]
    public void Invalid_creates_a_failed_result_with_error_details()
    {
        // Arrange
        // Act
        var result = Result.Invalid("Validation failed", "name", "Name is required");

        // Assert
        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.True(result.TryGetError(out var error));
        Assert.Equal("Validation failed", error.Detail);
        Assert.Equal(["Name is required"], error.ValidationErrors!["name"]);
    }

    [Fact]
    public void Ok_of_t_creates_a_successful_result_with_a_value()
    {
        // Arrange
        // Act
        var result = Result.Ok("porto");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal("porto", result.Value);
        Assert.Null(result.ErrorDetails);
    }

    [Fact]
    public void Created_creates_a_successful_non_generic_result()
    {
        // Arrange
        // Act
        var result = Result.Created("porto").ToResult();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(ResultStatus.Created, result.Status);
        Assert.Null(result.ErrorDetails);
    }

    [Fact]
    public void TryGetValue_returns_true_for_successful_generic_results()
    {
        // Arrange
        var result = Result.Ok("porto");

        // Act
        var hasValue = result.TryGetValue(out var value);

        // Assert
        Assert.True(hasValue);
        Assert.Equal("porto", value);
    }

    [Fact]
    public void Error_of_t_creates_a_failed_result_without_a_value()
    {
        // Arrange
        // Act
        var result = Result.Error<string>("Unexpected failure");

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.True(result.TryGetError(out var error));
        Assert.Equal("Unexpected failure", error.Detail);
    }

    [Fact]
    public void TryGetError_returns_false_for_successful_results()
    {
        // Arrange
        var result = Result.Ok();

        // Act
        var hasError = result.TryGetError(out var error);

        // Assert
        Assert.False(hasError);
        Assert.Null(error);
    }

    [Fact]
    public void TryGetError_returns_true_for_failed_generic_results()
    {
        // Arrange
        var result = Result.Error<string>("Unexpected failure");

        // Act
        var hasError = result.TryGetError(out var error);

        // Assert
        Assert.True(hasError);
        Assert.Equal("Unexpected failure", error!.Detail);
    }
}
