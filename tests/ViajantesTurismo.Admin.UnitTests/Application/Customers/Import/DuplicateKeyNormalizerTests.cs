using ViajantesTurismo.Common.Sanitizers;

namespace ViajantesTurismo.Admin.UnitTests.Application.Customers.Import;

public class DuplicateKeyNormalizerTests
{
    [Fact]
    public void Normalize_uses_field_specific_rules_for_name_and_email()
    {
        // Arrange
        const string nameWithDiacritics = "  José da Silva  ";
        const string nameWithoutDiacritics = "jose da silva";
        const string emailWithDiacritics = "  josé.silva@example.com  ";
        const string emailWithoutDiacritics = "jose.silva@example.com";

        // Act
        var normalizedNameWithDiacritics = StringSanitizer.NormalizeKeyRemovingDiacritics(nameWithDiacritics);
        var normalizedNameWithoutDiacritics = StringSanitizer.NormalizeKeyRemovingDiacritics(nameWithoutDiacritics);
        var normalizedEmailWithDiacritics = StringSanitizer.NormalizeKey(emailWithDiacritics);
        var normalizedEmailWithoutDiacritics = StringSanitizer.NormalizeKey(emailWithoutDiacritics);

        // Assert
        Assert.Equal(normalizedNameWithDiacritics, normalizedNameWithoutDiacritics);
        Assert.NotEqual(normalizedEmailWithDiacritics, normalizedEmailWithoutDiacritics);
    }
}
