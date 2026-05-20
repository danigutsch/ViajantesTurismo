namespace SharedKernel.Results.Tests;

[Trait(TestTraits.CapabilityName, TestTraits.ResultCapability)]
[Trait(TestTraits.CategoryName, TestTraits.CompositionCategory)]
[Trait(TestTraits.TheoryName, TestTraits.MatchSemanticsTheory)]
public sealed class ResultMatchTests
{
    [Fact]
    public void Returns_The_Success_Branch_Value_For_Generic_Results()
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
    public void Returns_The_Failure_Branch_Value_For_Generic_Results()
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
    public void Returns_The_Success_Branch_Value_For_Non_Generic_Results()
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
    public void Returns_The_Failure_Branch_Value_For_Non_Generic_Results()
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
