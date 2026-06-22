namespace SharedKernel.AspNet;

/// <summary>
/// Describes the required metadata for a mapped endpoint.
/// </summary>
public sealed class EndpointMetadata
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EndpointMetadata"/> class.
    /// </summary>
    /// <param name="name">The stable endpoint name used for link generation and OpenAPI operation IDs.</param>
    /// <param name="summary">The short endpoint summary used by API documentation.</param>
    /// <param name="description">The detailed endpoint description used by API documentation.</param>
    public EndpointMetadata(string name, string summary, string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(summary);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        Name = name;
        Summary = summary;
        Description = description;
    }

    /// <summary>
    /// Gets the stable endpoint name used for link generation and OpenAPI operation IDs.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the short endpoint summary used by API documentation.
    /// </summary>
    public string Summary { get; }

    /// <summary>
    /// Gets the detailed endpoint description used by API documentation.
    /// </summary>
    public string Description { get; }
}
