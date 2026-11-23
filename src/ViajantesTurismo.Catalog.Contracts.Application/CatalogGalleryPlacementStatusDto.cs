namespace ViajantesTurismo.Catalog.Contracts.Application;

/// <summary>The status of an accepted Catalog gallery placement.</summary>
public enum CatalogGalleryPlacementStatusDto
{
    /// <summary>The journal accepted the placement and it is waiting to resolve.</summary>
    Pending = 0,

    /// <summary>The tour event stream applied or acknowledged the placement.</summary>
    Applied = 1,

    /// <summary>Publication superseded an accepted draft-only placement.</summary>
    RejectedAfterPublication = 2,

    /// <summary>Retirement closed the tour before the placement resolved.</summary>
    ClosedByRetirement = 3,
}
