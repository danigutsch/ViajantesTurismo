namespace SharedKernel.Results.Tests;

[Trait(TestTraits.CapabilityName, TestTraits.ResultCapability)]
[Trait(TestTraits.CategoryName, TestTraits.EdgeCaseCategory)]
public sealed class ResultEdgeCaseTests
{
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
    public void Ok_of_t_throws_when_given_a_null_reference()
    {
        // Arrange
        // Act
        var exception = Assert.Throws<ArgumentNullException>(() => Result.Ok<string>(null!));

        // Assert
        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void Created_of_t_throws_when_given_a_null_reference()
    {
        // Arrange
        // Act
        var exception = Assert.Throws<ArgumentNullException>(() => Result.Created<string>(null!));

        // Assert
        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void Accepted_of_t_throws_when_given_a_null_reference()
    {
        // Arrange
        // Act
        var exception = Assert.Throws<ArgumentNullException>(() => Result.Accepted<string>(null!));

        // Assert
        Assert.Equal("value", exception.ParamName);
    }
}
