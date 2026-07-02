using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization.Metadata;

namespace ViajantesTurismo.Common.Contracts;

/// <summary>
/// Reads validation problem responses for contract-owned API clients.
/// </summary>
public static class ContractHttpValidation
{
    /// <summary>
    /// Ensures the response is successful or throws an exception containing validation errors.
    /// </summary>
    /// <param name="response">The HTTP response.</param>
    /// <param name="jsonTypeInfo">The validation problem JSON metadata.</param>
    /// <param name="ct">Cancellation token for the request.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task EnsureSuccessOrThrowValidationException(
        HttpResponseMessage response,
        JsonTypeInfo<ContractValidationProblemDto> jsonTypeInfo,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(jsonTypeInfo);

        if (response.IsSuccessStatusCode)
        {
            return;
        }

        if (response.StatusCode != HttpStatusCode.BadRequest)
        {
            response.EnsureSuccessStatusCode();
            return;
        }

        var problem = await response.Content.ReadFromJsonAsync(jsonTypeInfo, ct).ConfigureAwait(false);
        if (problem?.Errors is null || problem.Errors.Count == 0)
        {
            response.EnsureSuccessStatusCode();
            return;
        }

        var message = string.Join(Environment.NewLine, problem.Errors.SelectMany(error => error.Value));
        throw new ContractValidationException(message, problem.Errors);
    }
}
