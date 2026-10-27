using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ViajantesTurismo.Admin.Infrastructure.Payments;

namespace ViajantesTurismo.Admin.Infrastructure.Tests.Payments;

/// <summary>Arranges a temporary key directory and two self-signed certificates to exercise payment key-ring protection and rotation.</summary>
internal sealed class PaymentKeyRingScenario : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "vt-payment-keys-" + Guid.CreateVersion7().ToString("N"));
    private string Keys => Path.Combine(_directory, "keys");
    /// <summary>Creates the temporary key directory and writes the first and second test certificates as PFX files.</summary>
    public PaymentKeyRingScenario()
    {
        Directory.CreateDirectory(Keys);
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=VT payment test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(TimeProvider.System.GetUtcNow().AddDays(-1), TimeProvider.System.GetUtcNow().AddDays(30));
        File.WriteAllBytes(Path.Combine(_directory, "first.pfx"), certificate.Export(X509ContentType.Pfx));
        using var secondRsa = RSA.Create(2048);
        var secondRequest = new CertificateRequest("CN=VT rotated payment test", secondRsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var second = secondRequest.CreateSelfSigned(TimeProvider.System.GetUtcNow().AddDays(-1), TimeProvider.System.GetUtcNow().AddDays(30));
        File.WriteAllBytes(Path.Combine(_directory, "second.pfx"), second.Export(X509ContentType.Pfx));
    }
    /// <summary>Opens payment protection configured with the first certificate, or the second when <paramref name="rotated" /> is true, optionally trusting the first as a previous certificate.</summary>
    public PaymentProtection Open(bool rotated = false, bool retainPrevious = true)
    {
        var entries = new Dictionary<string, string?>
        {
            ["Payments:Enabled"] = "true",
            ["Payments:Protection:KeyDirectory"] = Keys,
            ["Payments:Protection:CertificatePath"] = Path.Combine(_directory, rotated ? "second.pfx" : "first.pfx")
        };
        if (rotated && retainPrevious)
        {
            entries["Payments:Protection:PreviousCertificates:0:Path"] = Path.Combine(_directory, "first.pfx");
        }
        return new PaymentProtection(new ConfigurationBuilder().AddInMemoryCollection(entries).Build());
    }
    /// <summary>Creates a new data-protection key in the key directory protected by the second certificate.</summary>
    public void RotateKeys()
    {
        using var certificate = X509CertificateLoader.LoadPkcs12FromFile(Path.Combine(_directory, "second.pfx"), null, X509KeyStorageFlags.EphemeralKeySet);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection().SetApplicationName("ViajantesTurismo.Admin.Payments").PersistKeysToFileSystem(new DirectoryInfo(Keys)).ProtectKeysWithCertificate(certificate);
        using var provider = services.BuildServiceProvider();
        var now = TimeProvider.System.GetUtcNow();
        provider.GetRequiredService<IKeyManager>().CreateNewKey(now, now.AddDays(90));
    }
    /// <summary>Moves the persisted key files to a backup folder and leaves an empty key directory.</summary>
    public void RemoveHistoricalKeys()
    {
        Directory.Move(Keys, Path.Combine(_directory, "historical-backup"));
        Directory.CreateDirectory(Keys);
    }
    /// <summary>Reads the XML key documents currently persisted in the key directory.</summary>
    public string[] ReadKeyDocuments() => [.. Directory.GetFiles(Keys, "*.xml").Select(File.ReadAllText)];
    /// <summary>Deletes the temporary directory with its keys and certificates.</summary>
    public void Dispose() => Directory.Delete(_directory, true);
}
