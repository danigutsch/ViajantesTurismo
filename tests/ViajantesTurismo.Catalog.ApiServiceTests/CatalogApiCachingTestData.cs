using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Contracts;
using ViajantesTurismo.Catalog.Domain.PublicContent;

namespace ViajantesTurismo.Catalog.ApiServiceTests;

internal static class CatalogApiCachingTestData
{
    public static CatalogTourDraftReadModel CreatePublishedTour(Guid tourId, string title, string slug)
    {
        return new CatalogTourDraftReadModel(
            tourId,
            Guid.CreateVersion7(),
            "CACHE-TOUR-1",
            title,
            slug,
            true,
            1,
            DateTimeOffset.UtcNow);
    }

    public static UpsertCatalogTourPresentationRequest CreatePresentationRequest(string title, string slug)
    {
        return new UpsertCatalogTourPresentationRequest
        {
            Title = title,
            Slug = slug,
            IsPublished = true
        };
    }

    public static CatalogTourPresentationUpdate CreatePresentationUpdate(string title, string slug)
    {
        var update = CatalogTourPresentationUpdate.Create(title, slug, isPublished: true);
        if (update.IsFailure)
        {
            throw new InvalidOperationException(update.ErrorDetails.Detail);
        }

        return update.Value;
    }

    public static EditablePublicContent CreatePublishedContent(string title)
    {
        var enUs = PublicContentVariant.Create(
            PublicContentLanguage.EnUs,
            title,
            "Ride with us",
            null,
            null,
            null,
            requiresHumanReview: false);
        if (enUs.IsFailure)
        {
            throw new InvalidOperationException(enUs.ErrorDetails.Detail);
        }

        var ptBr = PublicContentVariant.Create(
            PublicContentLanguage.PtBr,
            title,
            "Pedale conosco",
            null,
            null,
            null,
            requiresHumanReview: false);
        if (ptBr.IsFailure)
        {
            throw new InvalidOperationException(ptBr.ErrorDetails.Detail);
        }

        var content = EditablePublicContent.Create("home.hero", PublicContentLanguage.EnUs, [enUs.Value, ptBr.Value]);
        if (content.IsFailure)
        {
            throw new InvalidOperationException(content.ErrorDetails.Detail);
        }

        var publish = content.Value.Publish();
        if (publish.IsFailure)
        {
            throw new InvalidOperationException(publish.ErrorDetails.Detail);
        }

        return content.Value;
    }

    public static UpsertPublicContentRequest CreateContentRequest(string title)
    {
        return new UpsertPublicContentRequest
        {
            SourceLanguage = PublicContentLanguageDto.EnUs,
            Variants =
            {
                new PublicContentVariantDto { Language = PublicContentLanguageDto.EnUs, Title = title, Body = "Ride with us" },
                new PublicContentVariantDto { Language = PublicContentLanguageDto.PtBr, Title = title, Body = "Pedale conosco" }
            }
        };
    }
}
