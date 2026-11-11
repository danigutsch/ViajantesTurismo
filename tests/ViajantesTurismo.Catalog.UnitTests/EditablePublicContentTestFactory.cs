using PandaBear.BuildingBlocks;
using ViajantesTurismo.Catalog.Domain.PublicContent;

namespace ViajantesTurismo.Catalog.UnitTests;

/// <summary>Factory for valid editable public content in tests.</summary>
internal static class EditablePublicContentTestFactory
{
    /// <summary>Creates content with en-US and pt-BR variants; pt-BR carries the human review flag.</summary>
    public static EditablePublicContent CreateContent(bool requiresHumanReview, string key = "home.hero")
    {
        var enUs = CreateVariant(PublicContentCultures.EnglishUnitedStates, requiresHumanReview: false);
        var ptBr = CreateVariant(PublicContentCultures.PortugueseBrazil, requiresHumanReview);
        var result = EditablePublicContent.Create(key, PublicContentCultures.EnglishUnitedStates, [enUs, ptBr]);

        result.IsSuccess.ShouldBeTrue();
        return result.Value;
    }

    /// <summary>Creates a valid variant for the language with fixed sample text.</summary>
    public static PublicContentVariant CreateVariant(ContentCulture language, bool requiresHumanReview)
    {
        var result = PublicContentVariant.Create(
            language,
            "Welcome",
            "Discover cycling tours.",
            "Welcome",
            "Discover cycling tours in Brazil.",
            "Cycling tours in Brazil",
            requiresHumanReview);

        result.IsSuccess.ShouldBeTrue();
        return result.Value;
    }
}
