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

    [Fact]
    public void Map_satisfies_the_functor_identity_law()
    {
        // Arrange
        var option = Option.Some("porto");

        // Act
        var mapped = option.Map(static value => value);

        // Assert
        Assert.Equal(option, mapped);
    }

    [Fact]
    public void Map_satisfies_the_functor_composition_law()
    {
        // Arrange
        var option = Option.Some("porto");
        static string first(string value) => value.Trim();
        static string second(string value) => value.ToUpperInvariant();

        // Act
        var composed = option.Map(value => second(first(value)));
        var chained = option.Map(first).Map(second);

        // Assert
        Assert.Equal(composed, chained);
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
    public void Bind_satisfies_the_monad_left_identity_law()
    {
        // Arrange
        const string value = "porto";

        // Act
        var left = Option.Some(value).Bind(static city => Option.Some(city.ToUpperInvariant()));
        var right = Option.Some(value.ToUpperInvariant());

        // Assert
        Assert.Equal(right, left);
    }

    [Fact]
    public void Bind_satisfies_the_monad_right_identity_law()
    {
        // Arrange
        var option = Option.Some("porto");

        // Act
        var bound = option.Bind(Option.Some);

        // Assert
        Assert.Equal(option, bound);
    }

    [Fact]
    public void Bind_satisfies_the_monad_associativity_law()
    {
        // Arrange
        var option = Option.Some("porto");

        // Act
        var left = option
            .Bind(static city => Option.Some(city.Trim()))
            .Bind(static city => Option.Some(city.ToUpperInvariant()));
        var right = option.Bind(static city =>
            Option.Some(city.Trim()).Bind(static trimmed => Option.Some(trimmed.ToUpperInvariant())));

        // Assert
        Assert.Equal(left, right);
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
    public void Some_supports_value_types_when_the_value_is_not_null()
    {
        // Arrange
        const int expectedValue = 42;

        // Act
        var option = Option.Some(expectedValue);

        // Assert
        Assert.True(option.HasValue);
        Assert.Equal(expectedValue, option.Value);
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
