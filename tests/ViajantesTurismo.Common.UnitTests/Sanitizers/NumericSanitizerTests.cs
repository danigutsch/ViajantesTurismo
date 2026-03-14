using ViajantesTurismo.Common.Sanitizers;

namespace ViajantesTurismo.Common.UnitTests.Sanitizers;

public sealed class NumericSanitizerTests
{
    [Fact]
    public void Sanitize_Price_Rounds_Very_Small_Values()
    {
        var result = NumericSanitizer.SanitizePrice(0.001m);

        Assert.Equal(0.00m, result);
    }

    [Fact]
    public void Sanitize_Price_Rounds_Very_Small_Values_Up()
    {
        var result = NumericSanitizer.SanitizePrice(0.009m);

        Assert.Equal(0.01m, result);
    }

    [Fact]
    public void Sanitize_Price_Handles_Banker_Rounding_Case_Positive_Even()
    {
        // Arrange
        // Act
        var result = NumericSanitizer.SanitizePrice(2.225m);

        Assert.Equal(2.23m, result);
    }

    [Fact]
    public void Sanitize_Price_Handles_Banker_Rounding_Case_Positive_Odd()
    {
        // Arrange
        // Act
        var result = NumericSanitizer.SanitizePrice(2.215m);

        Assert.Equal(2.22m, result);
    }

    [Fact]
    public void Sanitize_Price_Handles_Floating_Point_Precision_Issue()
    {
        // Arrange
        // Act
        var result = NumericSanitizer.SanitizePrice(0.1m + 0.2m);

        Assert.Equal(0.30m, result);
    }

    [Theory]
    [InlineData(10.00, 10.00)]
    [InlineData(10.01, 10.01)]
    [InlineData(10.10, 10.10)]
    [InlineData(10.99, 10.99)]
    [InlineData(10.994, 10.99)]
    [InlineData(10.995, 11.00)]
    [InlineData(10.996, 11.00)]
    public void Sanitize_Price_Rounds_Correctly_For_Various_Inputs(decimal input, decimal expected)
    {
        var result = NumericSanitizer.SanitizePrice(input);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(1.001, 1.00)]
    [InlineData(1.004, 1.00)]
    [InlineData(1.005, 1.01)]
    [InlineData(1.006, 1.01)]
    [InlineData(1.009, 1.01)]
    public void Sanitize_Price_Rounds_Third_Decimal_Place_Correctly(decimal input, decimal expected)
    {
        var result = NumericSanitizer.SanitizePrice(input);

        Assert.Equal(expected, result);
    }
}
