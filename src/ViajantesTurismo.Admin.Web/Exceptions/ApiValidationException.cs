using System.Net;

namespace ViajantesTurismo.Admin.Web.Exceptions;

/// <summary>
/// Exception thrown when API returns validation errors (400 Bad Request with ValidationProblemDetails).
/// </summary>
public class ApiValidationException : HttpRequestException
{
    /// <summary>
    /// Initialises a new instance of the <see cref="ApiValidationException"/> class. Prefer to use <see cref="ApiValidationException(string, IDictionary{string, string[]})"/> instead.
    /// </summary>
    public ApiValidationException() : base("Validation failed", null, HttpStatusCode.BadRequest)
    {
        ValidationErrors = new Dictionary<string, string[]>();
    }

    /// <summary>
    /// Initialises a new instance of the <see cref="ApiValidationException"/> class with a message. Prefer to use <see cref="ApiValidationException(string, IDictionary{string, string[]})"/> instead.
    /// </summary>
    /// <param name="message">The error message.</param>
    public ApiValidationException(string message)
        : base(message, null, HttpStatusCode.BadRequest)
    {
        ValidationErrors = new Dictionary<string, string[]>();
    }

    /// <summary>
    /// Initialises a new instance of the <see cref="ApiValidationException"/> class with a message and inner exception. Prefer to use <see cref="ApiValidationException(string, IDictionary{string, string[]})"/> instead.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ApiValidationException(string message, Exception innerException) : base(message, innerException, HttpStatusCode.BadRequest)
    {
        ValidationErrors = new Dictionary<string, string[]>();
    }

    /// <summary>
    /// Initialises a new instance of the <see cref="ApiValidationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="validationErrors">The validation errors from the API.</param>
    public ApiValidationException(string message, IDictionary<string, string[]> validationErrors) : base(message, null, HttpStatusCode.BadRequest)
    {
        ArgumentNullException.ThrowIfNull(validationErrors);

        ValidationErrors = new Dictionary<string, string[]>(validationErrors);
    }

    /// <summary>
    /// Dictionary mapping field names to their validation error messages.
    /// </summary>
    public IReadOnlyDictionary<string, string[]> ValidationErrors { get; }

    /// <summary>
    /// Gets all validation error messages as a single formatted string.
    /// </summary>
    public string GetAllErrorMessages()
    {
        var messages = ValidationErrors
            .SelectMany(kvp => kvp.Value.Select(error => $"{kvp.Key}: {error}"))
            .ToList();

        return string.Join(Environment.NewLine, messages);
    }
}
