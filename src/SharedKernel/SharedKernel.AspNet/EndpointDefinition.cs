namespace SharedKernel.AspNet;

/// <summary>
/// Describes the required route pattern and metadata for a mapped endpoint.
/// </summary>
public sealed class EndpointDefinition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EndpointDefinition"/> class.
    /// </summary>
    /// <param name="pattern">The route pattern relative to the containing route group.</param>
    /// <param name="metadata">The required endpoint metadata.</param>
    public EndpointDefinition(string pattern, EndpointMetadata metadata)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);
        ArgumentNullException.ThrowIfNull(metadata);

        Pattern = pattern;
        Metadata = metadata;
    }

    /// <summary>
    /// Gets the route pattern relative to the containing route group.
    /// </summary>
    public string Pattern { get; }

    /// <summary>
    /// Gets the required endpoint metadata.
    /// </summary>
    public EndpointMetadata Metadata { get; }
}
