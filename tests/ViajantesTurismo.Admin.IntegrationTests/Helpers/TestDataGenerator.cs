namespace ViajantesTurismo.Admin.IntegrationTests.Helpers;

/// <summary>
/// Helper methods for generating unique test data to avoid collisions in parallel tests.
/// </summary>
internal static class TestDataGenerator
{
    /// <summary>
    /// Generates a unique email address for testing.
    /// </summary>
    public static string UniqueEmail(string prefix = "test")
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        return $"{prefix}.{suffix}@example.com";
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
    /// Generates a unique phone number for testing.
    /// </summary>
    public static string UniquePhone(string prefix = "+1555")
    {
        var suffix = Random.Shared.Next(1000000, 9999999);
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

    /// <summary>
    /// Generates a unique reference number for testing.
    /// </summary>
    public static string UniqueReferenceNumber(string prefix = "REF")
    {
        var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        return $"{prefix}-{suffix}";
    }
}
