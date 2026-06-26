namespace ViajantesTurismo.Management.WebTests.Components.Pages.Catalog;

internal static class PublicContentTestsHelpers
{
    public static PublicContentDto CreateContent(string key)
    {
        return new PublicContentDto
        {
            Key = key,
            SourceLanguage = PublicContentLanguageDto.EnUs,
            EnUs = new PublicContentVariantDto
            {
                Language = PublicContentLanguageDto.EnUs,
                Title = "Welcome",
                Body = "Ride with us"
            },
            PtBr = new PublicContentVariantDto
            {
                Language = PublicContentLanguageDto.PtBr,
                Title = "Bem-vindo",
                Body = "Pedale conosco",
                RequiresHumanReview = true
            },
            PublicationState = "ReviewRequired"
        };
    }
}
