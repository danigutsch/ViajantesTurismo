namespace SharedKernel.Results.Tests;

[Trait(TestTraits.CapabilityName, TestTraits.ResultCapability)]
[Trait(TestTraits.CategoryName, TestTraits.EdgeCaseCategory)]
public sealed class ResultEdgeCaseTests
{
    [Fact]
    public void Throws_When_The_Value_Is_Accessed_On_A_Failed_Result()
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
    public void Returns_False_For_Failed_Generic_Results()
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
    public void Throws_When_Ok_Gets_A_Null_Reference()
    {
        // Arrange
        // Act
        var exception = Assert.Throws<ArgumentNullException>(() => Result.Ok<string>(null!));

        // Assert
        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void Throws_When_Created_Gets_A_Null_Reference()
    {
        // Arrange
        // Act
        var exception = Assert.Throws<ArgumentNullException>(() => Result.Created<string>(null!));

        // Assert
        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void Throws_When_Accepted_Gets_A_Null_Reference()
    {
        // Arrange
        // Act
        var exception = Assert.Throws<ArgumentNullException>(() => Result.Accepted<string>(null!));

        // Assert
        Assert.Equal("value", exception.ParamName);
    }
}
