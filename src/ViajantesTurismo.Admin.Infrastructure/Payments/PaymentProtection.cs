using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using PandaBear.AspNetCore;

namespace ViajantesTurismo.Admin.Infrastructure.Payments;

internal sealed class PaymentProtection : IDisposable
{
    private readonly IDataProtectionProvider? _provider;
    private readonly List<X509Certificate2> _certificates = [];
    public bool Enabled
    {
        get;
    }
    public KeyBoundDataProtector Accounts => Create("ViajantesTurismo.Admin.PaymentAccounts.v1");
    public KeyBoundDataProtector Ledgers => Create("ViajantesTurismo.Admin.PaymentHistory.v1");
    public KeyBoundDataProtector InstructionSnapshots => Create("ViajantesTurismo.Admin.PaymentInstructionSnapshots.v1");
    public KeyBoundDataProtector InstructionArtifacts => Create("ViajantesTurismo.Admin.PaymentInstructionArtifacts.v1");

    public PaymentProtection(IConfiguration configuration)
    {
        Enabled = configuration.GetValue<bool>("Payments:Enabled");
        var keyDirectory = configuration["Payments:Protection:KeyDirectory"];
        if (!Enabled && string.IsNullOrWhiteSpace(keyDirectory))
        {
            return;
        }
        try
        {
            var certificatePath = configuration["Payments:Protection:CertificatePath"];
            if (string.IsNullOrWhiteSpace(keyDirectory) || !Path.IsPathFullyQualified(keyDirectory)
                || !Directory.Exists(keyDirectory) || string.IsNullOrWhiteSpace(certificatePath))
            {
                throw new InvalidOperationException("Payments require a persistent key directory and an encryption certificate.");
            }
            _certificates.Add(Load(certificatePath, configuration["Payments:Protection:CertificatePassword"]));
            foreach (var previous in configuration.GetSection("Payments:Protection:PreviousCertificates").GetChildren())
            {
                _certificates.Add(Load(previous["Path"], previous["Password"]));
            }
            _provider = DataProtectionProvider.Create(new DirectoryInfo(keyDirectory), builder => builder
                .SetApplicationName("ViajantesTurismo.Admin.Payments")
                .ProtectKeysWithCertificate(_certificates[0])
                .UnprotectKeysWithAnyCertificate([.. _certificates]));
            // Fail startup rather than enabling workflows with unusable key protection.
            var probe = Accounts.Protect("startup-check", [1]);
            _ = Accounts.Unprotect("startup-check", probe);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    internal PaymentProtection(IDataProtectionProvider provider)
    {
        _provider = provider;
        Enabled = true;
    }

    public void Dispose()
    {
        (_provider as IDisposable)?.Dispose();
        foreach (var certificate in _certificates)
        {
            certificate.Dispose();
        }
        _certificates.Clear();
    }

    private KeyBoundDataProtector Create(string purpose) => _provider is null
        ? throw new InvalidOperationException("Payment workflows are disabled.")
        : new KeyBoundDataProtector(_provider, purpose);

    private static X509Certificate2 Load(string? path, string? password)
    {
        if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path))
        {
            throw new InvalidOperationException("Payment encryption certificate configuration is required.");
        }
        var certificate = X509CertificateLoader.LoadPkcs12FromFile(path, password, X509KeyStorageFlags.EphemeralKeySet);
        if (certificate.HasPrivateKey)
        {
            return certificate;
        }
        certificate.Dispose();
        throw new CryptographicException("Payment encryption certificates require their private keys.");
    }
}
