namespace ViajantesTurismo.Catalog.Domain.Tours;

/// <summary>Records the complete photo order and chosen cover of a Catalog tour album.</summary>
public sealed record CatalogTourGalleryChanged
{
    /// <summary>Creates a durable gallery snapshot with an immutable photo sequence.</summary>
    /// <param name="catalogTourId">The Catalog tour identifier.</param>
    /// <param name="photoIds">Distinct photo identifiers in display order.</param>
    /// <param name="coverId">The editor's chosen cover, or null to use the first eligible photo.</param>
    /// <param name="intentId">The optional durable placement intent identity.</param>
    /// <param name="intentSequence">The matching per-tour sequence.</param>
    /// <param name="legacyBootstrapThroughSequence">The journal high-water mark captured by the initial legacy snapshot.</param>
    public CatalogTourGalleryChanged(
        Guid catalogTourId, IReadOnlyList<Guid> photoIds, Guid? coverId, Guid? intentId = null, long? intentSequence = null,
        long? legacyBootstrapThroughSequence = null)
    {
        ArgumentNullException.ThrowIfNull(photoIds);

        if (catalogTourId == Guid.Empty)
        {
            throw new ArgumentException("A gallery change requires a Catalog tour identifier.", nameof(catalogTourId));
        }

        var snapshot = photoIds.ToArray();
        if (snapshot.Contains(Guid.Empty) || snapshot.Distinct().Count() != snapshot.Length)
        {
            throw new ArgumentException("Gallery photos require distinct nonempty identifiers.", nameof(photoIds));
        }

        if (coverId is { } cover && !snapshot.Contains(cover))
        {
            throw new ArgumentException("The chosen cover must belong to the gallery.", nameof(coverId));
        }

        if (intentId.HasValue != intentSequence.HasValue || intentId == Guid.Empty || intentSequence is <= 0)
        {
            throw new ArgumentException("A placement acknowledgement requires an identity and a positive sequence.", nameof(intentId));
        }

        if (legacyBootstrapThroughSequence is < 0 || (legacyBootstrapThroughSequence.HasValue && intentId.HasValue))
        {
            throw new ArgumentException("A bootstrap captures a nonnegative watermark before replaying any placement intent.",
                nameof(legacyBootstrapThroughSequence));
        }

        CatalogTourId = catalogTourId;
        PhotoIds = Array.AsReadOnly(snapshot);
        CoverId = coverId;
        IntentId = intentId;
        IntentSequence = intentSequence;
        LegacyBootstrapThroughSequence = legacyBootstrapThroughSequence;
    }

    /// <summary>Gets the related Catalog tour.</summary>
    public Guid CatalogTourId
    {
        get;
    }

    /// <summary>Gets the photos in album order.</summary>
    public IReadOnlyList<Guid> PhotoIds
    {
        get;
    }

    /// <summary>Gets the chosen cover, when one was selected.</summary>
    public Guid? CoverId
    {
        get;
    }

    /// <summary>Gets the identity of the committed placement intent, when present.</summary>
    public Guid? IntentId
    {
        get;
    }

    /// <summary>Gets the per-tour sequence of the committed placement intent, when present.</summary>
    public long? IntentSequence
    {
        get;
    }

    /// <summary>Gets the captured journal watermark on the legacy bootstrap event.</summary>
    public long? LegacyBootstrapThroughSequence
    {
        get;
    }
}
