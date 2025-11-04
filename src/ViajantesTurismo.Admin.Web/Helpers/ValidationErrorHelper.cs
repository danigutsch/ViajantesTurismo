using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using ViajantesTurismo.Admin.Web.Exceptions;

namespace ViajantesTurismo.Admin.Web.Helpers;

/// <summary>
/// Helper class for parsing server validation errors from HTTP responses.
/// </summary>
internal static class ValidationErrorHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Ensures the response is successful, or parses validation errors and throws ApiValidationException.
    /// </summary>
    /// <param name="response">The HTTP response to check.</param>
    /// <exception cref="ApiValidationException">Thrown if the response contains validation errors.</exception>
    /// <exception cref="HttpRequestException">Thrown if the response is not successful and not a validation error.</exception>
    public static async Task EnsureSuccessOrThrowValidationException(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        if (response.StatusCode != HttpStatusCode.BadRequest)
        {
            response.EnsureSuccessStatusCode();
            return;
        }

        try
        {
            var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(JsonOptions);

            if (problemDetails?.Errors is not null && problemDetails.Errors.Count > 0)
            {
                var firstError = problemDetails.Errors.First();
                var errorMessage = $"Validation failed: {firstError.Key} - {string.Join(", ", firstError.Value)}";
                throw new ApiValidationException(errorMessage, problemDetails.Errors);
            }
        }
        catch (JsonException)
        {
            // Failed to parse as ValidationProblemDetails, fallback to standard error
        }

        response.EnsureSuccessStatusCode();
    }
}
