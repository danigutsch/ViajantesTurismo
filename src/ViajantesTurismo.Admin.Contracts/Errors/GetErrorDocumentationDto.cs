namespace ViajantesTurismo.Admin.Contracts;

/// <summary>
/// DTO describing one generated centralized error-documentation entry.
/// </summary>
public sealed record GetErrorDocumentationDto
{
    /// <summary>
    /// Gets the stable generated identifier for the error entry.
    /// </summary>
    public required string Identifier { get; init; }

    /// <summary>
    /// Gets the canonical repository documentation path for the error entry.
    /// </summary>
    public required string DocumentationPath { get; init; }

    /// <summary>
    /// Gets the fully qualified error provider type name.
    /// </summary>
    public required string ProviderType { get; init; }

    /// <summary>
    /// Gets the error provider member name.
    /// </summary>
    public required string MemberName { get; init; }

    /// <summary>
    /// Gets the result status name produced by the provider member.
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Gets the HTTP status code conventionally associated with the result status.
    /// </summary>
    public required int HttpStatusCode { get; init; }

    /// <summary>
    /// Gets the stable machine-readable error code.
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Gets the generated detail template derived from the result factory call.
    /// </summary>
    public required string DetailTemplate { get; init; }

    /// <summary>
    /// Gets the optional XML-doc summary for the provider member.
    /// </summary>
    public string? Summary { get; init; }
}
