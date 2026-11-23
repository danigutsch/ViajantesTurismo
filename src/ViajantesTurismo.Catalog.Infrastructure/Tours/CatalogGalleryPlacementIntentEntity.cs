namespace ViajantesTurismo.Catalog.Infrastructure.Tours;

/// <summary>Persists one independently acknowledged gallery placement intent.</summary>
internal sealed class CatalogGalleryPlacementIntentEntity
{
    /// <summary>Gets the stable identity of the local wake-up and the corresponding intent.</summary>
    public Guid Id
    {
        get;
        init;
    }

    /// <summary>Gets the affected tour.</summary>
    public Guid CatalogTourId
    {
        get;
        init;
    }

    /// <summary>Gets the assigned per-tour sequence.</summary>
    public long Sequence
    {
        get;
        init;
    }

    /// <summary>Gets the complete authored photo order, or the photo to append.</summary>
    public required Guid[] PhotoIds
    {
        get;
        init;
    }

    /// <summary>Gets the optional chosen cover.</summary>
    public Guid? CoverId
    {
        get;
        init;
    }

    /// <summary>Gets the caller's publication authority at acceptance.</summary>
    public bool CanPublish
    {
        get;
        init;
    }

    /// <summary>Gets whether this intent appends a single upload.</summary>
    public bool AppendsUploadedPhoto
    {
        get;
        init;
    }

    /// <summary>Gets the durable bootstrap wake receipt recorded at this captured high-water mark.</summary>
    public Guid? BootstrapWakeId
    {
        get;
        init;
    }
}
