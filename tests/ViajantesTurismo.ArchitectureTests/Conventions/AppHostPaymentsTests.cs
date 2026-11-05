using System.Security.Cryptography.X509Certificates;
using ViajantesTurismo.Resources;
using static ViajantesTurismo.ArchitectureTests.Conventions.AppHostPaymentsTestsHelpers;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

[Trait(PandaBear.Testing.TestTraitNames.AreaName, TestTraits.AppHostArea)]
public sealed class AppHostPaymentsTests : IDisposable
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
    public async Task Run_enables_payments_with_generated_protection_material()
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
    public async Task Run_reuses_the_generated_certificate_and_key_ring()
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
    public async Task Run_replaces_an_unusable_certificate_and_backs_up_its_unreadable_key_ring()
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
        Directory.GetFiles(_directory, "stale-key.xml", SearchOption.AllDirectories).Length.ShouldBe(1);
    }

    [Fact]
    public async Task Run_renews_a_certificate_close_to_expiry_and_keeps_the_old_key_ring_as_a_backup()
    {
        // Arrange
        var first = CreateBuilder(_directory);
        AppHost.AppHostComposition.AddProductResources(first, HostedProfile.Full);
        var firstEnvironment = await ApiEnvironment(first);
        var certificatePath = firstEnvironment["Payments__Protection__CertificatePath"];
        var keyDirectory = firstEnvironment["Payments__Protection__KeyDirectory"];
        await File.WriteAllTextAsync(Path.Combine(keyDirectory, "old-key.xml"), "old", TestContext.Current.CancellationToken);
        WriteCertificateValidFor(certificatePath, days: 5);
        var second = CreateBuilder(_directory);

        // Act
        AppHost.AppHostComposition.AddProductResources(second, HostedProfile.Full);
        _ = await ApiEnvironment(second);

        // Assert
        using var certificate = X509CertificateLoader.LoadPkcs12FromFile(certificatePath, null);
        certificate.NotAfter.ToUniversalTime().ShouldBeGreaterThan(TimeProvider.System.GetUtcNow().UtcDateTime.AddYears(1));
        File.Exists(Path.Combine(keyDirectory, "old-key.xml")).ShouldBeFalse();
        Directory.GetDirectories(_directory, "keys.bak-*").Length.ShouldBe(1);
        File.Exists(Path.Combine(Directory.GetDirectories(_directory, "keys.bak-*")[0], "old-key.xml")).ShouldBeTrue();
        Directory.GetFiles(_directory, "*.tmp").ShouldBeEmpty();
    }

    [Fact]
    public async Task Configured_payments_are_left_untouched()
    {
        // Arrange
        var builder = CreateBuilder(_directory);
        builder.Configuration["Payments:Enabled"] = "false";

        // Act
        AppHost.AppHostComposition.AddProductResources(builder, HostedProfile.Full);
        var environment = await ApiEnvironment(builder);

        // Assert
        environment.Keys.ShouldNotContain(key => key.StartsWith("Payments__", StringComparison.Ordinal));
        Directory.Exists(_directory).ShouldBeFalse();
    }

    [Fact]
    public void Publishing_never_generates_protection_material()
    {
        // Arrange
        var builder = CreatePublishBuilder(_directory);

        // Act
        AppHost.AppHostComposition.AddProductResources(builder, HostedProfile.Full);

        // Assert
        Directory.Exists(_directory).ShouldBeFalse();
    }

    [Theory]
    [InlineData(HostedProfile.Full)]
    [InlineData(HostedProfile.System)]
    [InlineData(HostedProfile.Admin)]
    public async Task Every_profile_enables_payments_in_a_run(HostedProfile profile)
    {
        // Arrange
        var builder = CreateBuilder(_directory);

        // Act
        AppHost.AppHostComposition.AddProductResources(builder, profile);
        var environment = await ApiEnvironment(builder);

        // Assert
        environment["Payments__Enabled"].ShouldBe("true");
    }
}
