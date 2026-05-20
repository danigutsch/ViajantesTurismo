namespace SharedKernel.Results.Tests;

[Trait(TestTraits.CapabilityName, TestTraits.OptionCapability)]
[Trait(TestTraits.CategoryName, TestTraits.CompositionCategory)]
[Trait(TestTraits.TheoryName, TestTraits.MatchSemanticsTheory)]
public sealed class OptionCompositionTests
{
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

    [Fact]
    public void Map_returns_none_when_the_option_is_empty()
    {
        // Arrange
        var option = Option.None<string>();

        // Act
        var mapped = option.Map(static value => value.ToUpperInvariant());

        // Assert
        Assert.Equal(Option.None<string>(), mapped);
    }

    [Fact]
    public void Bind_returns_none_when_the_option_is_empty()
    {
        // Arrange
        var option = Option.None<string>();

        // Act
        var bound = option.Bind(static value => Option.Some(value.ToUpperInvariant()));

        // Assert
        Assert.Equal(Option.None<string>(), bound);
    }

    [Fact]
    public void Map_can_project_reference_options_into_value_type_options()
    {
        // Arrange
        var option = Option.Some("porto");

        // Act
        var mapped = option.Map(static value => value.Length);

        // Assert
        Assert.Equal(Option.Some(5), mapped);
    }

    [Fact]
    public void Bind_can_project_into_value_type_options()
    {
        // Arrange
        var option = Option.Some("porto");

        // Act
        var bound = option.Bind(static value => Option.Some(value.Length));

        // Assert
        Assert.Equal(Option.Some(5), bound);
    }
}
