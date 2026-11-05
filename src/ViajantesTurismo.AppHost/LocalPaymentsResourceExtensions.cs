using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;

namespace ViajantesTurismo.AppHost;

/// <summary>Enables the payment workflow for the local developer run with machine-local protection material.</summary>
/// <remarks>
/// The workflow stays disabled by default in deployments. A developer run needs it enabled to exercise the Payment
/// accounts and booking finance pages, and the protection material it requires (key ring directory and encryption
/// certificate) is generated here rather than provisioned by hand. Nothing generated is a secret of any
/// deployment: the certificate is self-signed, has no password and lives only in the developer's local data directory.
/// </remarks>
internal static class LocalPaymentsResourceExtensions
{
    private const string DirectoryConfigurationKey = "Payments:LocalDevelopmentDirectory";
    private const string DirectoryName = "payments-development";
    private const string ApplicationDirectoryName = "ViajantesTurismo";
    private const string KeyRingDirectoryName = "keys";
    private const string CertificateFileName = "payments-development.pfx";
    private const string CertificateSubject = "CN=ViajantesTurismo local development payments";
    private const string BusinessTimeZone = "UTC";
    private const int CertificateYears = 10;
    private const int RenewalWindowDays = 30;

    /// <summary>Enables payments on the Admin API with generated, reusable protection material in run mode.</summary>
    /// <param name="api">The Admin API resource.</param>
    /// <returns>The same resource builder.</returns>
    public static IResourceBuilder<ProjectResource> WithLocalPayments(this IResourceBuilder<ProjectResource> api)
    {
        ArgumentNullException.ThrowIfNull(api);

        if (!api.ApplicationBuilder.ExecutionContext.IsRunMode)
        {
            return api;
        }

        var root = ResolveRoot(api.ApplicationBuilder.Configuration);
        var keyDirectory = Path.Combine(root, KeyRingDirectoryName);
        var certificatePath = Path.Combine(root, CertificateFileName);
        EnsureProtectionMaterial(root, keyDirectory, certificatePath);

        return api.WithEnvironment("Payments__Enabled", "true")
            .WithEnvironment("Payments__BusinessTimeZone", BusinessTimeZone)
            .WithEnvironment("Payments__Protection__KeyDirectory", keyDirectory)
            .WithEnvironment("Payments__Protection__CertificatePath", certificatePath);
    }

    private static string ResolveRoot(ConfigurationManager configuration)
    {
        var configured = configuration[DirectoryConfigurationKey];
        var root = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ApplicationDirectoryName, DirectoryName)
            : configured;
        return Path.GetFullPath(root);
    }

    private static void EnsureProtectionMaterial(string root, string keyDirectory, string certificatePath)
    {
        CreatePrivateDirectory(root);
        if (!HasUsableCertificate(certificatePath))
        {
            // A key ring is only readable with the certificate that protected it, so a replaced certificate
            // orphans its keys. This directory is generated here and holds no records worth keeping.
            if (Directory.Exists(keyDirectory))
            {
                Directory.Delete(keyDirectory, recursive: true);
            }

            WriteCertificate(certificatePath);
        }

        CreatePrivateDirectory(keyDirectory);
    }

    private static bool HasUsableCertificate(string path)
    {
        if (!File.Exists(path))
        {
            return false;
        }

        try
        {
            using var certificate = X509CertificateLoader.LoadPkcs12FromFile(path, null, X509KeyStorageFlags.EphemeralKeySet);
            var renewAfter = TimeProvider.System.GetUtcNow().AddDays(RenewalWindowDays).UtcDateTime;
            return certificate.HasPrivateKey && certificate.NotAfter.ToUniversalTime() > renewAfter;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    private static void WriteCertificate(string path)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(CertificateSubject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var now = TimeProvider.System.GetUtcNow();
        using var certificate = request.CreateSelfSigned(now.AddDays(-1), now.AddYears(CertificateYears));
        var options = new FileStreamOptions
        {
            Mode = FileMode.Create,
            Access = FileAccess.Write,
            Share = FileShare.None
        };
        if (!OperatingSystem.IsWindows())
        {
            options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        }

        using var stream = new FileStream(path, options);
        stream.Write(certificate.Export(X509ContentType.Pfx));
    }

    private static void CreatePrivateDirectory(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            Directory.CreateDirectory(path);
            return;
        }

        Directory.CreateDirectory(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
    }
}
