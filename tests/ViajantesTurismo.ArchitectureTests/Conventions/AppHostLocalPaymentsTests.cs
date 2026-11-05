using System.Security.Cryptography.X509Certificates;
using ViajantesTurismo.Resources;
using static ViajantesTurismo.ArchitectureTests.Conventions.AppHostLocalPaymentsTestsHelpers;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

[Trait(PandaBear.Testing.TestTraitNames.AreaName, TestTraits.AppHostArea)]
public sealed class AppHostLocalPaymentsTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"vt-local-payments-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public async Task Developer_run_enables_payments_with_generated_protection_material()
    {
        // Arrange
        var builder = CreateBuilder(_directory);

        // Act
        AppHost.AppHostComposition.AddProductResources(builder, HostedProfile.Full);
        var environment = await ApiEnvironment(builder);

        // Assert
        environment["Payments__Enabled"].ShouldBe("true");
        environment["Payments__BusinessTimeZone"].ShouldBe("UTC");
        Path.IsPathFullyQualified(environment["Payments__Protection__KeyDirectory"]).ShouldBeTrue();
        Directory.Exists(environment["Payments__Protection__KeyDirectory"]).ShouldBeTrue();
        using var certificate = X509CertificateLoader.LoadPkcs12FromFile(environment["Payments__Protection__CertificatePath"], null);
        certificate.HasPrivateKey.ShouldBeTrue();
        certificate.NotAfter.ToUniversalTime().ShouldBeGreaterThan(TimeProvider.System.GetUtcNow().UtcDateTime.AddYears(1));
    }

    [Fact]
    public async Task Developer_run_reuses_the_generated_certificate_and_key_ring()
    {
        // Arrange
        var first = CreateBuilder(_directory);
        AppHost.AppHostComposition.AddProductResources(first, HostedProfile.Full);
        var firstEnvironment = await ApiEnvironment(first);
        await File.WriteAllTextAsync(Path.Combine(firstEnvironment["Payments__Protection__KeyDirectory"], "marker.txt"), "kept", TestContext.Current.CancellationToken);
        var second = CreateBuilder(_directory);

        // Act
        AppHost.AppHostComposition.AddProductResources(second, HostedProfile.Full);
        var secondEnvironment = await ApiEnvironment(second);

        // Assert
        secondEnvironment["Payments__Protection__CertificatePath"].ShouldBe(firstEnvironment["Payments__Protection__CertificatePath"]);
        File.Exists(Path.Combine(secondEnvironment["Payments__Protection__KeyDirectory"], "marker.txt")).ShouldBeTrue();
        using var firstCertificate = X509CertificateLoader.LoadPkcs12FromFile(firstEnvironment["Payments__Protection__CertificatePath"], null);
        using var secondCertificate = X509CertificateLoader.LoadPkcs12FromFile(secondEnvironment["Payments__Protection__CertificatePath"], null);
        secondCertificate.Thumbprint.ShouldBe(firstCertificate.Thumbprint);
    }

    [Fact]
    public async Task Developer_run_replaces_an_unusable_certificate_and_its_unreadable_key_ring()
    {
        // Arrange
        var first = CreateBuilder(_directory);
        AppHost.AppHostComposition.AddProductResources(first, HostedProfile.Full);
        var firstEnvironment = await ApiEnvironment(first);
        var certificatePath = firstEnvironment["Payments__Protection__CertificatePath"];
        var keyDirectory = firstEnvironment["Payments__Protection__KeyDirectory"];
        var markerPath = Path.Combine(keyDirectory, "stale-key.xml");
        await File.WriteAllTextAsync(markerPath, "stale", TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(certificatePath, "not a pfx", TestContext.Current.CancellationToken);
        var second = CreateBuilder(_directory);

        // Act
        AppHost.AppHostComposition.AddProductResources(second, HostedProfile.Full);
        _ = await ApiEnvironment(second);

        // Assert
        using var certificate = X509CertificateLoader.LoadPkcs12FromFile(certificatePath, null);
        certificate.HasPrivateKey.ShouldBeTrue();
        File.Exists(markerPath).ShouldBeFalse();
        Directory.Exists(keyDirectory).ShouldBeTrue();
    }

    [Fact]
    public async Task Admin_only_profile_leaves_payments_to_its_own_configuration()
    {
        // Arrange
        var builder = CreateBuilder(_directory);

        // Act
        AppHost.AppHostComposition.AddProductResources(builder, HostedProfile.Admin);
        var environment = await ApiEnvironment(builder);

        // Assert
        environment.ContainsKey("Payments__Enabled").ShouldBeFalse();
        Directory.Exists(_directory).ShouldBeFalse();
    }
}
