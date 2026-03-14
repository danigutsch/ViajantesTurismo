namespace ViajantesTurismo.Admin.Tests.Shared.Integration.Helpers;

/// <summary>
/// Helper methods for generating unique test data to avoid collisions in parallel tests.
/// </summary>
public static class TestDataGenerator
{
    /// <summary>
    /// Generates a unique email address for testing.
    /// Removes spaces and invalid characters from the prefix to ensure valid email format.
    /// </summary>
    public static string UniqueEmail(string prefix = "test")
    {
        var cleanPrefix = prefix.Replace(" ", "", StringComparison.Ordinal).Replace("\t", "", StringComparison.Ordinal);
        var suffix = Guid.NewGuid().ToString("N")[..8];
        return $"{cleanPrefix}.{suffix}@example.com";
    }

    /// <summary>
    /// Generates a unique national ID for testing.
    /// </summary>
    public static string UniqueNationalId(string prefix = "ID")
    {
        var suffix = Random.Shared.Next(10000000, 99999999);
        return $"{prefix}{suffix}";
    }

    /// <summary>
    /// Generates a unique tour identifier for testing.
    /// </summary>
    public static string UniqueTourIdentifier(string prefix = "TOUR")
    {
        var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        return $"{prefix}{suffix}";
    }
}
