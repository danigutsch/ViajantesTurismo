using PandaBear.HttpClients;
using ViajantesTurismo.Management.Web.Helpers;

namespace ViajantesTurismo.Management.WebTests;

[Trait(PandaBear.Testing.TestTraitNames.AreaName, TestTraits.ManagementWebArea)]
[Trait(PandaBear.Testing.TestTraitNames.ScopeName, TestTraits.UnitScope)]
public sealed class HttpOutcomeUserMessageTests
{
    [Theory]
    [InlineData(HttpOutcomeKind.Unauthorized)]
    [InlineData(HttpOutcomeKind.Forbidden)]
    public void For_preserves_the_callers_existing_fallback_copy(HttpOutcomeKind kind)
    {
        // Arrange
        const string fallback = "The booking could not be loaded.";

        // Act
        var message = HttpOutcomeUserMessage.For(kind, fallback);

        // Assert
        message.ShouldBe(fallback);
    }
}
