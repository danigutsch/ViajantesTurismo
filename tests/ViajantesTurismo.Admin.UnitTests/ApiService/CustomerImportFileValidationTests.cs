using Microsoft.AspNetCore.Http;
using SharedKernel.Testing.Assertions;
using TestTraits = ViajantesTurismo.Admin.UnitTests.Infrastructure.TestTraits;
using ViajantesTurismo.Admin.ApiService.Customers;

namespace ViajantesTurismo.Admin.UnitTests.ApiService;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.SecurityCategory)]
public sealed class CustomerImportFileValidationTests
{
    [Fact]
    public void Accepts_small_csv_uploads()
    {
        // Arrange
        using var stream = new MemoryStream("firstName,lastName"u8.ToArray());
        var file = new FormFile(stream, 0, stream.Length, "file", "customers.csv")
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/csv"
        };

        // Act
        var isValid = CustomerImportFileValidation.TryValidate(file, out var problem);

        // Assert
        isValid.ShouldBeTrue();
        problem.ShouldBeNull();
    }

    [Fact]
    public void Rejects_empty_uploads_with_generic_problem_details()
    {
        // Arrange
        using var stream = new MemoryStream();
        var file = new FormFile(stream, 0, 0, "file", "customers.csv")
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/csv"
        };

        // Act
        var isValid = CustomerImportFileValidation.TryValidate(file, out var problem);

        // Assert
        isValid.ShouldBeFalse();
        problem.ShouldNotBeNull();
        problem.Title.ShouldBe("Invalid customer import file.");
        problem.Detail.ShouldBe("Upload a CSV file that meets the documented import requirements.");
    }

    [Fact]
    public void Rejects_non_csv_uploads()
    {
        // Arrange
        using var stream = new MemoryStream([1, 2, 3]);
        var file = new FormFile(stream, 0, stream.Length, "file", "customers.exe")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/octet-stream"
        };

        // Act
        var isValid = CustomerImportFileValidation.TryValidate(file, out var problem);

        // Assert
        isValid.ShouldBeFalse();
        problem.ShouldNotBeNull();
    }

    [Fact]
    public void Rejects_oversized_uploads()
    {
        // Arrange
        using var stream = new MemoryStream([1]);
        var file = new FormFile(stream, 0, CustomerImportFileValidation.MaxFileBytes + 1, "file", "customers.csv")
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/csv"
        };

        // Act
        var isValid = CustomerImportFileValidation.TryValidate(file, out var problem);

        // Assert
        isValid.ShouldBeFalse();
        problem.ShouldNotBeNull();
    }
}
