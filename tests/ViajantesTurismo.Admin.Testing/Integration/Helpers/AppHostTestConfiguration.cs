namespace ViajantesTurismo.Admin.Testing.Integration.Helpers;

/// <summary>
/// Holds test-only Aspire parameters and local Keycloak user credentials.
/// </summary>
public sealed record AppHostTestConfiguration(
    IReadOnlyList<string> Arguments,
    string ConformanceUserPassword,
    string OperatorConformanceUserPassword);
