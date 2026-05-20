namespace SharedKernel.Results.Tests;

[Trait(TestTraits.CapabilityName, TestTraits.OptionCapability)]
public sealed class OptionTests
{
    [Fact]
    public void Some_creates_an_option_with_a_value()
    {
        // Arrange
        const string expectedValue = "porto";

        // Act
        var option = Option.Some(expectedValue);

        // Assert
        Assert.True(option.HasValue);
        Assert.False(option.IsEmpty);
        Assert.Equal(expectedValue, option.Value);
    }

    [Fact]
    public void None_creates_an_empty_option()
    {
        // Arrange
        var option = Option.None<string>();

        // Assert
        Assert.False(option.HasValue);
        Assert.True(option.IsEmpty);
        Assert.Null(option.Value);
    }

    [Fact]
    public void Some_rejects_null_values()
    {
        // Act
        var exception = Assert.Throws<ArgumentNullException>(() => Option.Some<string>(null!));

        // Assert
        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void FromNullable_returns_none_for_null_values()
    {
        // Arrange
        string? value = null;

        // Act
        var option = Option.FromNullable(value);

        // Assert
        Assert.False(option.HasValue);
        Assert.True(option.IsEmpty);
    }

    [Fact]
    public void Match_returns_the_some_branch_value_when_a_value_is_present()
    {
        // Arrange
        var option = Option.Some("porto");

        // Act
        var result = option.Match(
            static value => value.ToUpperInvariant(),
            static () => "EMPTY");

        // Assert
        Assert.Equal("PORTO", result);
    }

    [Fact]
    public void Match_returns_the_none_branch_value_when_no_value_is_present()
    {
        // Arrange
        var option = Option.None<string>();

        // Act
        var result = option.Match(
            static value => value.ToUpperInvariant(),
            static () => "EMPTY");

        // Assert
        Assert.Equal("EMPTY", result);
    }
}
