using System.Net;

namespace SharedKernel.HttpClients;

/// <summary>
/// Represents the outcome of an HTTP request without a materialized response value.
/// </summary>
public sealed record HttpOutcome
{
    /// <summary>
    /// Gets the outcome kind.
    /// </summary>
    public required ContractCommandOutcomeKind Kind { get; init; }

    /// <summary>
    /// Gets the HTTP status code returned by the API, or <see langword="null" /> when no response was received.
    /// </summary>
    public HttpStatusCode? StatusCode { get; init; }

    /// <summary>
    /// Gets the created resource location when supplied by the API.
    /// </summary>
    public Uri? Location { get; init; }

    /// <summary>
    /// Gets validation errors when the API returned a validation problem.
    /// </summary>
    public IReadOnlyDictionary<string, string[]>? ValidationErrors { get; init; }

    /// <summary>
    /// Gets a diagnostic message suitable for logs or tests, not direct UI display.
    /// </summary>
    public string? Message { get; init; }
}
