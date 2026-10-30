using System.Security.Cryptography;
using System.Text;
using PandaBear.Testing;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Payments;

[Trait(SharedKernelTestTraitNames.CategoryName, SharedKernelTestTraitNames.UnitScope)]
[Trait(SharedKernelTestTraitNames.CapabilityName, TestTraits.PaymentsCapability)]
[TestScope(TestScope.Integration)]
public sealed class PaymentProtectionTests
{
    [Fact]
    public void Restart_and_certificate_rotation_retain_access_to_historical_ciphertext()
    {
        // Arrange
        using var scenario = new PaymentKeyRingScenario();
        var plaintext = Encoding.UTF8.GetBytes("DE89370400440532013000");
        byte[] oldCiphertext;
        using (var first = scenario.Open())
        {
            oldCiphertext = first.Accounts.Protect("account", plaintext);
        }
        scenario.RotateKeys();

        // Act
        using var restarted = scenario.Open(rotated: true);
        var restored = restarted.Accounts.Unprotect("account", oldCiphertext);
        var newCiphertext = restarted.Accounts.Protect("account", plaintext);

        // Assert
        restored.ShouldBe(plaintext);
        newCiphertext.ShouldNotBe(oldCiphertext);
        scenario.ReadKeyDocuments().All(xml => xml.Contains("encryptedSecret", StringComparison.Ordinal)).ShouldBeTrue();
        Encoding.UTF8.GetString(newCiphertext).ShouldNotContain("DE89370400440532013000", StringComparison.Ordinal);
        using var restartedAgain = scenario.Open(rotated: true);
        restartedAgain.Accounts.Unprotect("account", newCiphertext).ShouldBe(plaintext);
        restartedAgain.Accounts.Unprotect("account", oldCiphertext).ShouldBe(plaintext);
    }

    [Fact]
    public void Missing_historical_keys_fail_closed_instead_of_returning_unprotected_data()
    {
        // Arrange
        using var scenario = new PaymentKeyRingScenario();
        byte[] ciphertext;
        using (var first = scenario.Open())
        {
            ciphertext = first.Accounts.Protect("account", [1, 2, 3]);
        }
        scenario.RemoveHistoricalKeys();
        using var restarted = scenario.Open();

        // Act
        var read = () => restarted.Accounts.Unprotect("account", ciphertext);

        // Assert
        read.ShouldThrow<CryptographicException>();
    }

    [Fact]
    public void Protection_is_bound_to_both_record_identity_and_separate_admin_purposes()
    {
        // Arrange
        using var scenario = new PaymentKeyRingScenario();
        using var protection = scenario.Open();
        var ciphertext = protection.Accounts.Protect("first-account", [1, 2, 3]);

        // Act
        var otherRecord = () => protection.Accounts.Unprotect("second-account", ciphertext);
        var otherPurpose = () => protection.InstructionSnapshots.Unprotect("first-account", ciphertext);

        // Assert
        otherRecord.ShouldThrow<CryptographicException>();
        otherPurpose.ShouldThrow<CryptographicException>();
    }
}
