namespace ViajantesTurismo.Management.WebTests.Components.Pages.Catalog;

/// <summary>Test data helpers for public content page tests.</summary>
internal static class PublicContentTestsHelpers
{
    /// <summary>Builds a review-required content entry with an en-US variant and a pt-BR variant that needs human review.</summary>
    public static PublicContentDto CreateContent(string key)
    {
        var content = new PublicContentDto
        {
            Key = key,
            SourceLanguage = PublicContentLanguageDto.EnUs,
            PublicationState = "ReviewRequired"
        };

        content.Variants.Add(new PublicContentVariantDto { Language = PublicContentLanguageDto.EnUs, Title = "Welcome", Body = "Ride with us" });
        content.Variants.Add(new PublicContentVariantDto { Language = PublicContentLanguageDto.PtBr, Title = "Bem-vindo", Body = "Pedale conosco", RequiresHumanReview = true });

        return content;
    }
}
