namespace ViajantesTurismo.Admin.Contracts;

/// <summary>
/// Describes the HTTP outcome of a customer creation request without applying UI fallback policy.
/// </summary>
public enum CustomerCreateOutcomeKind
{
    /// <summary>
    /// The customer was created successfully.
    /// </summary>
    Succeeded,

    /// <summary>
    /// The API returned a validation problem response.
    /// </summary>
    ValidationProblem,

    /// <summary>
    /// The API returned not found.
    /// </summary>
    NotFound,

    /// <summary>
    /// The API returned unauthorized.
    /// </summary>
    Unauthorized,

    /// <summary>
    /// The API returned forbidden.
    /// </summary>
    Forbidden,

    /// <summary>
    /// The API returned conflict.
    /// </summary>
    Conflict,

    /// <summary>
    /// The API returned a response that required a body but the body was empty.
    /// </summary>
    EmptyBody,

    /// <summary>
    /// The API returned a response body that could not be deserialized into the expected contract.
    /// </summary>
    MalformedBody,

    /// <summary>
    /// The API returned a status code not explicitly modeled by this client contract.
    /// </summary>
    UnexpectedStatus
}
