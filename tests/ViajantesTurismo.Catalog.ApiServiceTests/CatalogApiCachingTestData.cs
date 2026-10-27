using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Catalog.Domain.PublicContent;

namespace ViajantesTurismo.Catalog.ApiServiceTests;

/// <summary>Builds catalog tour, presentation and public-content fixtures for API caching tests.</summary>
internal static class CatalogApiCachingTestData
{
    /// <summary>Creates a published tour draft read model with the given title and slug.</summary>
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
            TimeProvider.System.GetUtcNow())
        {
            Summary = $"Summary for {title}",
            StreamVersion = 3
        };
    }

    /// <summary>Creates a tour presentation upsert request with expected version 3.</summary>
    public static UpsertCatalogTourPresentationRequest CreatePresentationRequest(string title, string slug)
    {
        return new UpsertCatalogTourPresentationRequest
        {
            Title = title,
            Slug = slug,
            Summary = $"Summary for {title}",
            ExpectedVersion = 3
        };
    }

    /// <summary>Creates a valid tour presentation update; throws when creation fails.</summary>
    public static CatalogTourPresentationUpdate CreatePresentationUpdate(string title, string slug)
    {
        var update = CatalogTourPresentationUpdate.Create(
            title,
            slug,
            $"Summary for {title}",
            description: null,
            itinerary: null,
            seoTitle: null,
            seoDescription: null);
        if (update.IsFailure)
        {
            throw new InvalidOperationException(update.ErrorDetails.Detail);
        }

        return update.Value;
    }

    /// <summary>Creates and publishes home hero content with English and Portuguese variants; throws on failure.</summary>
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

    /// <summary>Creates an upsert request with English and Portuguese variants for the given title.</summary>
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
