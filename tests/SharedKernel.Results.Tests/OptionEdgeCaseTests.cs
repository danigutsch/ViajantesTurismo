namespace SharedKernel.Results.Tests;

[Trait(TestTraits.CapabilityName, TestTraits.OptionCapability)]
[Trait(TestTraits.CategoryName, TestTraits.EdgeCaseCategory)]
public sealed class OptionEdgeCaseTests
{
    [Fact]
    public void Rejects_Null_Values()
    {
        // Act
        var exception = Assert.Throws<ArgumentNullException>(() => Option.Some<string>(null!));

        // Assert
        Assert.Equal("value", exception.ParamName);
    }
}
