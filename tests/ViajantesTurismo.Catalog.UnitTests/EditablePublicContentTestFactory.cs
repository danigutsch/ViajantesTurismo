using ViajantesTurismo.Catalog.Domain.PublicContent;

namespace ViajantesTurismo.Catalog.UnitTests;

internal static class EditablePublicContentTestFactory
{
    public static EditablePublicContent CreateContent(bool requiresHumanReview)
    {
        var enUs = CreateVariant(PublicContentLanguage.EnUs, requiresHumanReview: false);
        var ptBr = CreateVariant(PublicContentLanguage.PtBr, requiresHumanReview);
        var result = EditablePublicContent.Create("home.hero", PublicContentLanguage.EnUs, enUs, ptBr);

        Assert.True(result.IsSuccess);
        return result.Value;
    }

    public static PublicContentVariant CreateVariant(PublicContentLanguage language, bool requiresHumanReview)
    {
        var result = PublicContentVariant.Create(
            language,
            "Welcome",
            "Discover cycling tours.",
            "Welcome",
            "Discover cycling tours in Brazil.",
            "Cycling tours in Brazil",
            requiresHumanReview);

        Assert.True(result.IsSuccess);
        return result.Value;
    }
}
