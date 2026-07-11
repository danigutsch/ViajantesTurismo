using System.ComponentModel.DataAnnotations;
using ViajantesTurismo.Catalog.Contracts.Application;

namespace ViajantesTurismo.Catalog.UnitTests;

public static class CatalogContractValidationTestsHelpers
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

    public static IReadOnlyList<ValidationResult> Validate<T>(T value)
    {
        var errors = new List<ValidationResult>();
        var context = new ValidationContext(value ?? throw new ArgumentNullException(nameof(value)));
        Validator.TryValidateObject(value, context, errors, validateAllProperties: true);

        return errors;
    }

    public static void ShouldContainErrorFor(this IReadOnlyList<ValidationResult> errors, string memberName)
    {
        errors.Any(error => error.MemberNames.Contains(memberName)).ShouldBeTrue();
    }

    public static void ShouldNotContainErrorFor(this IReadOnlyList<ValidationResult> errors, string memberName)
    {
        errors.Any(error => error.MemberNames.Contains(memberName)).ShouldBeFalse();
    }
}
