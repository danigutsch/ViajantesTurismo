using SharedKernel.HttpClients;
using ViajantesTurismo.Management.Web.Helpers;

namespace ViajantesTurismo.Management.WebTests;

[Trait(SharedKernel.Testing.TestTraitNames.AreaName, TestTraits.ManagementWebArea)]
[Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.UnitScope)]
public sealed class HttpOutcomeUserMessageTests
{
    [Theory]
    [InlineData(ContractCommandOutcomeKind.Unauthorized)]
    [InlineData(ContractCommandOutcomeKind.Forbidden)]
    public void For_preserves_the_callers_existing_fallback_copy(ContractCommandOutcomeKind kind)
    {
        // Arrange
        const string fallback = "The booking could not be loaded.";

        // Act
        var message = HttpOutcomeUserMessage.For(kind, fallback);

        // Assert
        message.ShouldBe(fallback);
    }
}
