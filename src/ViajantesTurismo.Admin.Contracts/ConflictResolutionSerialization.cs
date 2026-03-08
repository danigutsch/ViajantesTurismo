namespace ViajantesTurismo.Admin.Contracts;

/// <summary>
/// Serializes and parses conflict resolution maps exchanged between clients and customer import endpoints.
/// </summary>
public static class ConflictResolutionSerialization
{
    /// <summary>
    /// Serializes conflict resolutions using the <c>email=decision;email2=decision2</c> format.
    /// </summary>
    /// <param name="conflictResolutions">Conflict resolution dictionary keyed by email.</param>
    /// <returns>Serialized conflict resolution string.</returns>
    public static string Serialize(IReadOnlyDictionary<string, string> conflictResolutions)
    {
        ArgumentNullException.ThrowIfNull(conflictResolutions);

        if (conflictResolutions.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(
            ';',
            conflictResolutions.Select(kvp =>
                $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
    }

    /// <summary>
    /// Parses a serialized conflict resolution string into a case-insensitive dictionary.
    /// </summary>
    /// <param name="serializedConflictResolutions">Serialized conflict resolutions.</param>
    /// <returns>Parsed conflict resolutions keyed by email.</returns>
    public static Dictionary<string, string> Parse(string? serializedConflictResolutions)
    {
        if (string.IsNullOrWhiteSpace(serializedConflictResolutions))
        {
            return new(StringComparer.OrdinalIgnoreCase);
        }

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var pair in serializedConflictResolutions.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var separatorIndex = pair.IndexOf('=', StringComparison.Ordinal);
            if (separatorIndex <= 0 || separatorIndex == pair.Length - 1)
            {
                continue;
            }

            var email = Uri.UnescapeDataString(pair[..separatorIndex]);
            var decision = Uri.UnescapeDataString(pair[(separatorIndex + 1)..]);

            if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(decision))
            {
                result[email] = decision;
            }
        }

        return result;
    }
}
