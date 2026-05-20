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
        // Act
        var result = Result.Error<string>("Unexpected failure");

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.True(result.TryGetError(out var error));
        Assert.Equal("Unexpected failure", error.Detail);
    }

    [Fact]
    public void TryGetValue_returns_false_for_failed_generic_results()
    {
        // Arrange
        var result = Result.Error<string>("Unexpected failure");

        // Act
        var hasValue = result.TryGetValue(out var value);

        // Assert
        Assert.False(hasValue);
        Assert.Null(value);
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

    [Fact]
    public void Map_transforms_the_success_value()
    {
        // Arrange
        var result = Result.Ok("porto");

        // Act
        var mapped = result.Map(static value => value.Length);

        // Assert
        Assert.True(mapped.IsSuccess);
        Assert.Equal(5, mapped.Value);
    }

    [Fact]
    public void Map_preserves_failure_details()
    {
        // Arrange
        var result = Result.Error<string>("Unexpected failure");

        // Act
        var mapped = result.Map(static value => value.Length);

        // Assert
        Assert.True(mapped.IsFailure);
        Assert.True(mapped.TryGetError(out var error));
        Assert.Equal("Unexpected failure", error.Detail);
    }

    [Fact]
    public void Bind_flattens_successful_results()
    {
        // Arrange
        var result = Result.Ok("porto");

        // Act
        var bound = result.Bind(static value => Result.Ok(value.Length));

        // Assert
        Assert.True(bound.IsSuccess);
        Assert.Equal(5, bound.Value);
    }

    [Fact]
    public void Bind_short_circuits_failures()
    {
        // Arrange
        var result = Result.Error<string>("Unexpected failure");

        // Act
        var bound = result.Bind(static value => Result.Ok(value.Length));

        // Assert
        Assert.True(bound.IsFailure);
        Assert.True(bound.TryGetError(out var error));
        Assert.Equal("Unexpected failure", error.Detail);
    }

    [Fact]
    public void Match_returns_the_success_branch_value_for_generic_results()
    {
        // Arrange
        var result = Result.Ok("porto");

        // Act
        var matched = result.Match(
            static value => value.Length,
            static error => error.Detail.Length);

        // Assert
        Assert.Equal(5, matched);
    }

    [Fact]
    public void Match_returns_the_failure_branch_value_for_generic_results()
    {
        // Arrange
        var result = Result.Error<string>("Unexpected failure");

        // Act
        var matched = result.Match(
            static value => value.Length,
            static error => error.Detail.Length);

        // Assert
        Assert.Equal("Unexpected failure".Length, matched);
    }

    [Fact]
    public void Match_returns_the_success_branch_value_for_non_generic_results()
    {
        // Arrange
        var result = Result.Ok();

        // Act
        var matched = result.Match(
            static () => "success",
            static error => error.Detail);

        // Assert
        Assert.Equal("success", matched);
    }

    [Fact]
    public void Match_returns_the_failure_branch_value_for_non_generic_results()
    {
        // Arrange
        var result = Result.Error("Unexpected failure");

        // Act
        var matched = result.Match(
            static () => "success",
            static error => error.Detail);

        // Assert
        Assert.Equal("Unexpected failure", matched);
    }
}
