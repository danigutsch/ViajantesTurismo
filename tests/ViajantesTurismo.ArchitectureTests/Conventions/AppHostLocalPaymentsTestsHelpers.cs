using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

/// <summary>Helpers for tests of the local developer payment configuration in the AppHost.</summary>
internal static class AppHostLocalPaymentsTestsHelpers
{
    /// <summary>Configuration key that redirects generated payment material to a test directory.</summary>
    internal const string DirectoryConfigurationKey = "Payments:LocalDevelopmentDirectory";

    /// <summary>Creates a run-mode builder that keeps generated payment material in the given directory.</summary>
    internal static IDistributedApplicationBuilder CreateBuilder(string directory)
    {
        var builder = DistributedApplication.CreateBuilder([]);
        builder.Configuration[DirectoryConfigurationKey] = directory;
        return builder;
    }

    /// <summary>Creates a publish-mode builder that keeps generated payment material in the given directory.</summary>
    internal static IDistributedApplicationBuilder CreatePublishBuilder(string directory)
    {
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
        {
            Args = ["--operation", "publish", "--publisher", "manifest"],
        });
        builder.Configuration[DirectoryConfigurationKey] = directory;
        return builder;
    }

    /// <summary>Writes a passwordless PFX with a private key that stays valid for the given number of days.</summary>
    internal static void WriteCertificateValidFor(string path, int days)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=expiring", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var now = TimeProvider.System.GetUtcNow();
        using var certificate = request.CreateSelfSigned(now.AddDays(-1), now.AddDays(days));
        File.WriteAllBytes(path, certificate.Export(X509ContentType.Pfx));
    }

    /// <summary>
    /// Collects the plain-text environment variables the Admin API resource receives.
    /// </summary>
    /// <remarks>
    /// Runs the environment callbacks without resolving references, because a run-mode connection string waits for
    /// endpoints that only exist once the application is started.
    /// </remarks>
    internal static async Task<Dictionary<string, string>> ApiEnvironment(IDistributedApplicationBuilder builder)
    {
        var api = builder.Resources.OfType<ProjectResource>().Single(project => project.Name == ResourceNames.Api);
        var variables = new Dictionary<string, object>(StringComparer.Ordinal);
        var context = new EnvironmentCallbackContext(builder.ExecutionContext, api, variables);
        foreach (var annotation in api.Annotations.OfType<EnvironmentCallbackAnnotation>())
        {
            await annotation.Callback(context);
        }

        return variables
            .Where(static variable => variable.Value is string)
            .ToDictionary(static variable => variable.Key, static variable => (string)variable.Value, StringComparer.Ordinal);
    }
}
