namespace ViajantesTurismo.Admin.Testing.Integration.Helpers;

/// <summary>
/// Holds test-only Aspire parameters and the paired local Keycloak user credential.
/// </summary>
public sealed record AppHostTestConfiguration(IReadOnlyList<string> Arguments, string ConformanceUserPassword);
