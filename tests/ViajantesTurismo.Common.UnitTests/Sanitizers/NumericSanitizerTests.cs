using ViajantesTurismo.Common.Sanitizers;

namespace ViajantesTurismo.Common.UnitTests.Sanitizers;

public sealed class NumericSanitizerTests
{
    [Fact]
    public void Sanitize_Price_Returns_Zero_When_Input_Is_Zero()
    {
        var result = NumericSanitizer.SanitizePrice(0m);

        Assert.Equal(0m, result);
    }

    [Fact]
    public void Sanitize_Price_Preserves_Integer_Values()
    {
        var result = NumericSanitizer.SanitizePrice(100m);

        Assert.Equal(100m, result);
    }

    [Fact]
    public void Sanitize_Price_Preserves_One_Decimal_Place()
    {
        var result = NumericSanitizer.SanitizePrice(99.9m);

        Assert.Equal(99.9m, result);
    }

    [Fact]
    public void Sanitize_Price_Preserves_Two_Decimal_Places()
    {
        var result = NumericSanitizer.SanitizePrice(99.99m);

        Assert.Equal(99.99m, result);
    }

    [Fact]
    public void Sanitize_Price_Rounds_Three_Decimal_Places()
    {
        var result = NumericSanitizer.SanitizePrice(99.995m);

        Assert.Equal(100.00m, result);
    }

    [Fact]
    public void Sanitize_Price_Rounds_Down_When_Below_Midpoint()
    {
        var result = NumericSanitizer.SanitizePrice(99.994m);

        Assert.Equal(99.99m, result);
    }

    [Fact]
    public void Sanitize_Price_Rounds_Up_When_Above_Midpoint()
    {
        var result = NumericSanitizer.SanitizePrice(99.996m);

        Assert.Equal(100.00m, result);
    }

    [Fact]
    public void Sanitize_Price_Rounds_Midpoint_Away_From_Zero_For_Positive()
    {
        var result = NumericSanitizer.SanitizePrice(99.995m);

        Assert.Equal(100.00m, result);
    }

    [Fact]
    public void Sanitize_Price_Rounds_Midpoint_Away_From_Zero_For_Negative()
    {
        var result = NumericSanitizer.SanitizePrice(-99.995m);

        Assert.Equal(-100.00m, result);
    }

    [Fact]
    public void Sanitize_Price_Handles_Negative_Values()
    {
        var result = NumericSanitizer.SanitizePrice(-50.75m);

        Assert.Equal(-50.75m, result);
    }

    [Fact]
    public void Sanitize_Price_Handles_Small_Values()
    {
        var result = NumericSanitizer.SanitizePrice(0.01m);

        Assert.Equal(0.01m, result);
    }

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
    public void Sanitize_Price_Handles_Large_Values()
    {
        var result = NumericSanitizer.SanitizePrice(999999.99m);

        Assert.Equal(999999.99m, result);
    }

    [Fact]
    public void Sanitize_Price_Rounds_Large_Values_With_Extra_Decimals()
    {
        var result = NumericSanitizer.SanitizePrice(999999.999m);

        Assert.Equal(1000000.00m, result);
    }

    [Fact]
    public void Sanitize_Price_Handles_Maximum_Decimal_Value()
    {
        var result = NumericSanitizer.SanitizePrice(decimal.MaxValue);

        Assert.Equal(decimal.MaxValue, result);
    }

    [Fact]
    public void Sanitize_Price_Handles_Minimum_Decimal_Value()
    {
        var result = NumericSanitizer.SanitizePrice(decimal.MinValue);

        Assert.Equal(decimal.MinValue, result);
    }

    [Fact]
    public void Sanitize_Price_Rounds_Multiple_Trailing_Decimals()
    {
        var result = NumericSanitizer.SanitizePrice(12.3456789m);

        Assert.Equal(12.35m, result);
    }

    [Fact]
    public void Sanitize_Price_Handles_Banker_Rounding_Case_Positive_Even()
    {
        // 2.225 with AwayFromZero should round to 2.23
        var result = NumericSanitizer.SanitizePrice(2.225m);

        Assert.Equal(2.23m, result);
    }

    [Fact]
    public void Sanitize_Price_Handles_Banker_Rounding_Case_Positive_Odd()
    {
        // 2.215 with AwayFromZero should round to 2.22
        var result = NumericSanitizer.SanitizePrice(2.215m);

        Assert.Equal(2.22m, result);
    }

    [Fact]
    public void Sanitize_Price_Handles_Floating_Point_Precision_Issue()
    {
        // Common floating point issue: 0.1 + 0.2 = 0.30000000000000004
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
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(10000)]
    public void Sanitize_Price_Preserves_Integer_Values_Theory(decimal value)
    {
        var result = NumericSanitizer.SanitizePrice(value);

        Assert.Equal(value, result);
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

    [Fact]
    public void Sanitize_Price_Is_Idempotent()
    {
        var value = 99.999m;
        var firstRound = NumericSanitizer.SanitizePrice(value);
        var secondRound = NumericSanitizer.SanitizePrice(firstRound);

        Assert.Equal(firstRound, secondRound);
    }

    [Fact]
    public void Sanitize_Price_Handles_Currency_Typical_Values()
    {
        var result = NumericSanitizer.SanitizePrice(1234.5678m);

        Assert.Equal(1234.57m, result);
    }
}