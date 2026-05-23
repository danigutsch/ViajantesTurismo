using System.Diagnostics.CodeAnalysis;

namespace ViajantesTurismo.Admin.E2ETests.Infrastructure.Bases;

/// <summary>
/// Provides shared xUnit collection names used by E2E tests.
/// </summary>
[ExcludeFromCodeCoverage]
public static class E2ETestCollections
{
    /// <summary>
    /// Collection name for serial E2E tests.
    /// </summary>
    public const string Serial = "E2E.Serial";
}
