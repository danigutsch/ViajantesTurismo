namespace ViajantesTurismo.Management.WebTests.Components.Pages.Catalog;

internal static class PublicContentTestsHelpers
{
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
