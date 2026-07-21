using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.Catalog.Contracts.Application;

/// <summary>
/// Request to explicitly publish or unpublish a Catalog tour.
/// </summary>
public sealed record CatalogTourPublicationRequest
{
    /// <summary>
    /// Gets the stream version on which this transition is based.
    /// </summary>
    [Range(1, long.MaxValue)]
    public required long ExpectedVersion { get; init; }
}
