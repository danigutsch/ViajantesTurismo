namespace SharedKernel.Results.Tests;

[Trait(TestTraits.CapabilityName, TestTraits.OptionCapability)]
[Trait(TestTraits.CategoryName, TestTraits.EdgeCaseCategory)]
public sealed class OptionEdgeCaseTests
{
    [Fact]
    public void Some_rejects_null_values()
    {
        // Act
        var exception = Assert.Throws<ArgumentNullException>(() => Option.Some<string>(null!));

        // Assert
        Assert.Equal("value", exception.ParamName);
    }
}
