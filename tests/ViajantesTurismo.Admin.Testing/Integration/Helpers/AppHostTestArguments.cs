namespace ViajantesTurismo.Admin.Testing.Integration.Helpers;

/// <summary>
/// Provides non-production OIDC parameter values for test-created AppHosts.
/// </summary>
public static class AppHostTestArguments
{
    /// <summary>
    /// Creates command-line arguments that satisfy local Keycloak test parameters.
    /// </summary>
    /// <returns>The AppHost parameter arguments.</returns>
    public static string[] Create()
    {
        return [.. CreateConfiguration().Arguments];
    }

    /// <summary>
    /// Creates local Keycloak test parameters and retains the conformance-user credential for token requests.
    /// </summary>
    /// <returns>The test AppHost parameter configuration.</returns>
    public static AppHostTestConfiguration CreateConfiguration()
    {
        var secret = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));

        return new AppHostTestConfiguration(
            [
                $"--Parameters:management-web-client-secret={secret}",
                $"--Parameters:identity-provider-conformance-user-password={secret}",
                $"--Parameters:identity-provider-operator-conformance-user-password={secret}",
                $"--Parameters:identity-provider-admin-password={secret}"
            ],
            secret,
            secret);
    }
}
