using ViajantesTurismo.Common.Sanitizers;

namespace ViajantesTurismo.Admin.UnitTests.Application.Customers.Import;

public class DuplicateKeyNormalizerTests
{
    [Fact]
    public void Normalize_Uses_Field_Specific_Rules_For_Name_And_Email()
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
