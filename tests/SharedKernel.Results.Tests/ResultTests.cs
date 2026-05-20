namespace SharedKernel.Results.Tests;

[Trait(TestTraits.CapabilityName, TestTraits.ResultCapability)]
public sealed class ResultTests
{
    [Fact]
    public void Ok_creates_a_successful_result()
    {
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
        // Act
        var result = Result.Ok("porto");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal("porto", result.Value);
        Assert.Null(result.ErrorDetails);
    }

    [Fact]
    public void Error_of_t_creates_a_failed_result_without_a_value()
    {
        // Act
        var result = Result.Error<string>("Unexpected failure");

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.True(result.TryGetError(out var error));
        Assert.Equal("Unexpected failure", error.Detail);
    }

    [Fact]
    public void Value_throws_when_accessed_on_a_failed_result()
    {
        // Arrange
        var result = Result.Error<string>("Unexpected failure");

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () =>
            {
                _ = result.Value;
            });

        // Assert
        Assert.Contains("failed result", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ToResult_discards_the_value_and_preserves_success_status()
    {
        // Arrange
        var result = Result.Ok("porto");

        // Act
        var converted = result.ToResult();

        // Assert
        Assert.True(converted.IsSuccess);
        Assert.Equal(ResultStatus.Ok, converted.Status);
        Assert.Null(converted.ErrorDetails);
    }

    [Fact]
    public void ToResult_discards_the_value_and_preserves_failure_error()
    {
        // Arrange
        var result = Result.Conflict<string>("Tour is already published");

        // Act
        var converted = result.ToResult();

        // Assert
        Assert.True(converted.IsFailure);
        Assert.Equal(ResultStatus.Conflict, converted.Status);
        Assert.True(converted.TryGetError(out var error));
        Assert.Equal("Tour is already published", error.Detail);
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
