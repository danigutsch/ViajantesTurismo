namespace ViajantesTurismo.Admin.Testing;

/// <summary>Inspects document API responses for the repository-standard no-store headers.</summary>
public static class DocumentNoStoreHeaders
{
    private const string ExpiredAtUnixEpochHttpDate = "Thu, 01 Jan 1970 00:00:00 GMT";

    /// <summary>Determines whether the response prevents browser and intermediary storage.</summary>
    /// <param name="response">The response to inspect.</param>
    /// <returns><see langword="true" /> when all standard no-store headers are present.</returns>
    public static bool ArePresent(HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);

        IEnumerable<string> expires = [];
        if (response.Headers.NonValidated.TryGetValues("Expires", out var values)
            || response.Content.Headers.NonValidated.TryGetValues("Expires", out values))
        {
            expires = values;
        }

        return response.Headers.CacheControl?.NoStore is true
            && response.Headers.Pragma.Count == 1
            && string.Equals(response.Headers.Pragma.Single().Name, "no-cache", StringComparison.OrdinalIgnoreCase)
            && expires.Count() == 1
            && string.Equals(expires.Single(), ExpiredAtUnixEpochHttpDate, StringComparison.Ordinal);
    }
}
