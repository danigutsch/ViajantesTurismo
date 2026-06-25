using System.ComponentModel.DataAnnotations;
using ViajantesTurismo.Catalog.Contracts;

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
        Assert.Empty(errors);
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
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(CatalogTourDto.Identifier)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(CatalogTourDto.Title)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(CatalogTourDto.Slug)));
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
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(CatalogTourDto.Identifier)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(CatalogTourDto.Title)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(CatalogTourDto.Slug)));
    }

    [Fact]
    public void CatalogTourImageDto_rejects_invalid_text_lengths()
    {
        // Arrange
        var image = new CatalogTourImageDto
        {
            Uri = new Uri("https://cdn.example/tour.jpg"),
            AltText = string.Empty,
            Caption = new string('c', ContractConstants.MaxCaptionLength + 1)
        };

        // Act
        var errors = CatalogContractValidationTestsHelpers.Validate(image);

        // Assert
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(CatalogTourImageDto.AltText)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(CatalogTourImageDto.Caption)));
    }

    private static class CatalogContractValidationTestsHelpers
    {
        public static CatalogTourDto CreateTour()
        {
            return new CatalogTourDto
            {
                Id = Guid.CreateVersion7(),
                AdminTourId = Guid.CreateVersion7(),
                Identifier = "TOUR-2026",
                Title = "Tour 2026",
                Slug = "tour-2026",
                IsPublished = true,
                Images =
                [
                    new CatalogTourImageDto
                {
                    Uri = new Uri("https://cdn.example/tour.jpg"),
                    AltText = "Cyclists on a mountain road",
                    Caption = "Morning climb"
                }
                ],
                UpdatedAt = DateTimeOffset.UtcNow
            };
        }

        public static List<ValidationResult> Validate<T>(T value)
        {
            var errors = new List<ValidationResult>();
            var context = new ValidationContext(value ?? throw new ArgumentNullException(nameof(value)));
            Validator.TryValidateObject(value, context, errors, validateAllProperties: true);

            return errors;
        }
    }
}
