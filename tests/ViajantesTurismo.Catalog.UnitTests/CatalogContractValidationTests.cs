using ViajantesTurismo.Catalog.Contracts.Application;

namespace ViajantesTurismo.Catalog.UnitTests;

public sealed class CatalogContractValidationTests
{
    [Fact]
    public void CatalogTourDto_accepts_a_valid_public_contract()
    {
        // Arrange
        var tour = CatalogContractValidationTestsHelpers.CreateTour();

        // Act
        var errors = CatalogContractValidationTestsHelpers.Validate(tour);

        // Assert
        errors.ShouldBeEmpty();
    }

    [Fact]
    public void CatalogTourDto_rejects_empty_required_strings()
    {
        // Arrange
        var tour = CatalogContractValidationTestsHelpers.CreateTour() with
        {
            Identifier = string.Empty,
            Title = string.Empty,
            Slug = string.Empty
        };

        // Act
        var errors = CatalogContractValidationTestsHelpers.Validate(tour);

        // Assert
        errors.ShouldContainErrorFor(nameof(CatalogTourDto.Identifier));
        errors.ShouldContainErrorFor(nameof(CatalogTourDto.Title));
        errors.ShouldContainErrorFor(nameof(CatalogTourDto.Slug));
    }

    [Fact]
    public void CatalogTourDto_rejects_strings_that_exceed_contract_limits()
    {
        // Arrange
        var tour = CatalogContractValidationTestsHelpers.CreateTour() with
        {
            Identifier = new string('i', ContractConstants.MaxDefaultLength + 1),
            Title = new string('t', ContractConstants.MaxNameLength + 1),
            Slug = new string('s', ContractConstants.MaxSlugLength + 1)
        };

        // Act
        var errors = CatalogContractValidationTestsHelpers.Validate(tour);

        // Assert
        errors.ShouldContainErrorFor(nameof(CatalogTourDto.Identifier));
        errors.ShouldContainErrorFor(nameof(CatalogTourDto.Title));
        errors.ShouldContainErrorFor(nameof(CatalogTourDto.Slug));
    }

    [Fact]
    public void CatalogTourImageDto_rejects_invalid_text_lengths()
    {
        // Arrange
        var image = new CatalogTourImageDto
        {
            Id = Guid.CreateVersion7(),
            AltText = new string('a', ContractConstants.MaxAltTextLength + 1),
            Caption = new string('c', ContractConstants.MaxCaptionLength + 1),
            ResponsiveVariants =
            [
                new CatalogMediaImageVariantDto { Width = 640, Height = 427, ContentType = "image/jpeg", FileSizeBytes = 1024 }
            ]
        };

        // Act
        var errors = CatalogContractValidationTestsHelpers.Validate(image);

        // Assert
        errors.ShouldContainErrorFor(nameof(CatalogTourImageDto.AltText));
        errors.ShouldContainErrorFor(nameof(CatalogTourImageDto.Caption));
    }

    [Fact]
    public void CatalogTourImageDto_allows_empty_alt_text_for_decorative_images()
    {
        // Arrange
        var image = new CatalogTourImageDto
        {
            Id = Guid.CreateVersion7(),
            AltText = string.Empty,
            IsDecorative = true,
            ResponsiveVariants =
            [
                new CatalogMediaImageVariantDto { Width = 640, Height = 427, ContentType = "image/jpeg", FileSizeBytes = 1024 }
            ]
        };

        // Act
        var errors = CatalogContractValidationTestsHelpers.Validate(image);

        // Assert
        errors.ShouldNotContainErrorFor(nameof(CatalogTourImageDto.AltText));
    }
}
