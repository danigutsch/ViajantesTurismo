using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.Catalog.Contracts.Application;

/// <summary>Requests a complete gallery placement based on the last observed Catalog editor state.</summary>
public sealed record QueueCatalogGalleryPlacementRequest
{
    /// <summary>Gets the observed tour event-stream revision.</summary>
    [Range(1, long.MaxValue)]
    public required long StreamVersion
    {
        get;
        init;
    }

    /// <summary>Gets the observed latest accepted placement sequence, or zero for an empty journal.</summary>
    [Range(0, long.MaxValue)]
    public required long AcceptedPlacementSequence
    {
        get;
        init;
    }

    /// <summary>Gets the complete desired photo order. An empty list removes all authored placements.</summary>
    [Required]
    public required IReadOnlyList<Guid> PhotoIds
    {
        get;
        init;
    }

    /// <summary>Gets the optional explicit cover from the supplied photos.</summary>
    public Guid? CoverId
    {
        get;
        init;
    }
}
