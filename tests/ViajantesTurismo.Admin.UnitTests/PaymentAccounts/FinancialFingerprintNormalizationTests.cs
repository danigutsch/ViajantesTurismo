using TestTraits = ViajantesTurismo.Admin.UnitTests.Infrastructure.TestTraits;
using ViajantesTurismo.Admin.Application.Idempotency;
using System.Text.Json;
using ViajantesTurismo.Admin.Application.Payments;

namespace ViajantesTurismo.Admin.UnitTests.PaymentAccounts;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.CoreBehaviorCategory)]
[Trait(PandaBear.Testing.TestTraitNames.ScopeName, TestTraits.UnitScope)]
public sealed class FinancialFingerprintNormalizationTests
{
    [Theory]
    [InlineData("{\"b\":2.00,\"a\":1.0}", "{\"a\":1,\"b\":2}")]
    [InlineData("{\"outer\":{\"b\":2,\"a\":1.00}}", "{\"outer\":{\"a\":1,\"b\":2.0}}")]
    [InlineData("[1.0,2.00,null,true,\"text\"]", "[1,2,null,true,\"text\"]")]
    public void Object_property_order_and_decimal_scale_do_not_change_request_identity(string firstJson, string secondJson)
    {
        // Arrange
        using var first = JsonDocument.Parse(firstJson);
        using var second = JsonDocument.Parse(secondJson);

        // Act
        var left = PaymentCommandFingerprint.Create(Guid.Empty, null, "actor", first.RootElement, FinancialFingerprintJsonContext.Default.JsonElement);
        var right = PaymentCommandFingerprint.Create(Guid.Empty, null, "actor", second.RootElement, FinancialFingerprintJsonContext.Default.JsonElement);

        // Assert
        left.ShouldBe(right);
    }

    [Fact]
    public void Typed_payloads_preserve_the_existing_context_and_length_framed_hash_contract()
    {
        // Arrange
        using var input = JsonDocument.Parse("{\"amount\":100.00}");
        var expected = ApplicationRequestFingerprint.Create(Guid.Empty.ToString("N"), string.Empty, "actor", "{\"amount\":100}");

        // Act
        var actual = PaymentCommandFingerprint.Create(Guid.Empty, null, "actor", input.RootElement, FinancialFingerprintJsonContext.Default.JsonElement);
        var otherActor = PaymentCommandFingerprint.Create(Guid.Empty, null, "other", input.RootElement, FinancialFingerprintJsonContext.Default.JsonElement);

        // Assert
        actual.ShouldBe(expected);
        actual.ShouldNotBe(otherActor);
    }

    [Theory]
    [InlineData("[1,2]", "[2,1]")]
    [InlineData("{\"value\":\" a\"}", "{\"value\":\"a\"}")]
    [InlineData("{\"amount\":1}", "{\"amount\":2}")]
    public void Array_order_strings_and_changed_values_remain_significant(string firstJson, string secondJson)
    {
        // Arrange
        using var first = JsonDocument.Parse(firstJson);
        using var second = JsonDocument.Parse(secondJson);

        // Act
        var left = PaymentCommandFingerprint.Create(Guid.Empty, null, "actor", first.RootElement, FinancialFingerprintJsonContext.Default.JsonElement);
        var right = PaymentCommandFingerprint.Create(Guid.Empty, null, "actor", second.RootElement, FinancialFingerprintJsonContext.Default.JsonElement);

        // Assert
        left.ShouldNotBe(right);
    }
}
