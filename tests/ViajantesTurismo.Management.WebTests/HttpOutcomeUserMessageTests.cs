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

    [Fact]
    public void For_replaces_server_validation_detail_with_the_fixed_validation_copy()
    {
        // Arrange
        const string fallback = "Image could not be uploaded. Try again later.";
        const string validation = "Image could not be uploaded. Check the file and try again.";

        // Act
        var message = HttpOutcomeUserMessage.For(HttpOutcomeKind.ValidationProblem, fallback, validationProblem: validation);

        // Assert
        message.ShouldBe(validation);
    }

    [Fact]
    public void For_uses_the_fallback_for_a_validation_problem_without_validation_copy()
    {
        // Arrange
        const string fallback = "Image could not be uploaded. Try again later.";

        // Act
        var message = HttpOutcomeUserMessage.For(HttpOutcomeKind.ValidationProblem, fallback);

        // Assert
        message.ShouldBe(fallback);
    }
}
