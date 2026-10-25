using Microsoft.Extensions.Configuration;
using PandaBear.Testing;

namespace ViajantesTurismo.Management.WebTests;

[Trait(TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
[Trait(TestTraitNames.ScopeName, TestTraits.ComponentScope)]
public sealed class MediaLanguageGuidanceTests
{
    [Theory]
    [Trait(TestTraits.RequiresName, TestTraits.FastTextRequirement)]
    [InlineData("The cyclists are riding bicycles through the beautiful mountain valley.", "en-US", "match")]
    [InlineData("Um grupo de ciclistas atravessa uma estrada nas montanhas ao lado de um vale verde e tranquilo.", "en-US", "mismatch")]
    [InlineData("Um grupo de ciclistas atravessa uma estrada nas montanhas ao lado de um vale verde e tranquilo.", "pt-BR", "match")]
    [InlineData("The cyclists are riding along a mountain road. Os visitantes caminham pela praia durante o passeio.", "en-US", "indeterminate")]
    public async Task Uses_the_real_local_engine(string text, string tag, string expected)
    {
        // Arrange
        var (executable, model) = FastTextEngine.Require();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["MediaLanguageIdentification:ExecutablePath"] = executable,
            ["MediaLanguageIdentification:ModelPath"] = model
        }).Build();
        var service = new MediaLanguageGuidance(configuration);

        // Act
        var result = await service.Evaluate(text, tag, CancellationToken.None);

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("__label__en 0.99 __label__pt 0.01", 3, "indeterminate")]
    [InlineData("__label__en 0.99 __label__pt 0.01\n__label__en 0.99 __label__pt 0.01", 1, "indeterminate")]
    [InlineData("__label__en 0.99 __label__pt 0.01\n__label__pt 0.99 __label__en 0.01\n__label__en 0.99 __label__pt 0.01", 3, "indeterminate")]
    [InlineData("__label__en 0.99 __label__pt 0.01\n__label__en 0.99 __label__pt 0.01\n__label__en 0.99 __label__pt 0.01", 3, "match")]
    public void Requires_exactly_one_prediction_for_each_submitted_window(string output, int expectedCount, string expected)
    {
        // Act
        var result = MediaLanguageGuidance.InterpretOutput(output, "en-US", expectedCount);

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("__label__en 0.99 __label__pt 0.01", "en-US", "match")]
    [InlineData("__label__pt 0.99 __label__en 0.01", "en-US", "mismatch")]
    [InlineData("__label__pt 0.99 __label__en 0.01", "pt-BR", "match")]
    [InlineData("__label__en 0.55 __label__pt 0.45", "en-US", "indeterminate")]
    [InlineData("__label__de 0.99 __label__en 0.01", "en-US", "indeterminate")]
    [InlineData("invalid", "en-US", "indeterminate")]
    public void Interprets_confidence_and_primary_language_separately_from_declaration(string output, string tag, string expected)
    {
        // Act
        var result = MediaLanguageGuidance.InterpretPrediction(output, tag);

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("en-US", true)]
    [InlineData("pt-br", true)]
    [InlineData("en_US", false)]
    [InlineData("fr-FR", false)]
    [InlineData("", false)]
    public void Allows_only_supported_bcp47_declarations(string? tag, bool expected)
    {
        // Act
        var result = MediaLanguageGuidance.IsAllowedLanguageTag(tag);

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("Rio")]
    [InlineData("12345678901234567890123456789012345678901234567890")]
    [InlineData("😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀")]
    [InlineData("Maria Silva Pedro Santos Carlos Almeida Antonio Pereira")]
    public async Task Treats_short_numeric_emoji_and_proper_name_heavy_text_as_indeterminate(string text)
    {
        // Arrange
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["MediaLanguageIdentification:ExecutablePath"] = "not-an-absolute-path",
            ["MediaLanguageIdentification:ModelPath"] = "not-an-absolute-path"
        }).Build();
        var service = new MediaLanguageGuidance(configuration);

        // Act
        var result = await service.Evaluate(text, "en-US", CancellationToken.None);

        // Assert
        result.ShouldBe("indeterminate");
    }
}
