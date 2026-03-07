using ViajantesTurismo.Admin.Application.Import;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Common.Sanitizers;

namespace ViajantesTurismo.Admin.UnitTests.Application.Customers.Import;

public class DuplicateDetectorTests
{
    [Fact]
    public void FindDuplicateEmailLineNumbers_With_Duplicate_Email_Flags_Second_Row()
    {
        // Arrange
        var headers = new[] { "FirstName", "LastName", "Email" };
        var rows = new[]
        {
            CsvRow.Parse("John,Doe,john.doe@example.com"),
            CsvRow.Parse("Jane,Doe,  JOHN.DOE@example.com  ")
        };

        var documentResult = CsvDocument.Create(headers, rows);
        var document = documentResult.Value;

        // Act
        var duplicateLineNumbers = DuplicateDetector.FindDuplicateEmailLineNumbers(document);

        // Assert
        var duplicateLineNumber = Assert.Single(duplicateLineNumbers);
        Assert.Equal(3, duplicateLineNumber);
    }

    [Fact]
    public void FindDuplicateNameLineNumbers_With_Diacritics_Variation_Flags_Second_Row()
    {
        // Arrange
        var headers = new[] { "FirstName", "LastName", "Email" };
        var rows = new[]
        {
            CsvRow.Parse("José,Silva,jose.silva@example.com"),
            CsvRow.Parse("Jose,Silva,jose2.silva@example.com")
        };

        var documentResult = CsvDocument.Create(headers, rows);
        var document = documentResult.Value;

        // Act
        var duplicateLineNumbers = DuplicateDetector.FindDuplicateNameLineNumbers(document);

        // Assert
        var duplicateLineNumber = Assert.Single(duplicateLineNumbers);
        Assert.Equal(3, duplicateLineNumber);
    }

    [Fact]
    public async Task FindDuplicateEmailLineNumbersAgainstDatabaseAsync_With_Matching_Email_Flags_Row()
    {
        // Arrange
        var headers = new[] { "FirstName", "LastName", "Email" };
        var rows = new[]
        {
            CsvRow.Parse("John,Doe,john.doe@example.com")
        };

        var documentResult = CsvDocument.Create(headers, rows);
        var document = documentResult.Value;
        var store = new FakeCustomerStore(["JOHN.DOE@EXAMPLE.COM"]);

        // Act
        var duplicateLineNumbers = await DuplicateDetector.FindDuplicateEmailLineNumbersAgainstDatabaseAsync(
            document,
            store,
            CancellationToken.None);

        // Assert
        var duplicateLineNumber = Assert.Single(duplicateLineNumbers);
        Assert.Equal(2, duplicateLineNumber);
    }

    private sealed class FakeCustomerStore(IEnumerable<string> existingEmails) : ICustomerStore
    {
        private readonly HashSet<string> _existingEmails =
        [
            ..existingEmails.Select(StringSanitizer.NormalizeKey)
        ];

        public void Add(Customer customer) => throw new NotSupportedException();

        public Task<Customer?> GetById(Guid id, CancellationToken ct) => throw new NotSupportedException();

        public void Delete(Customer customer) => throw new NotSupportedException();

        public Task<bool> EmailExists(string email, CancellationToken ct) =>
            Task.FromResult(_existingEmails.Contains(StringSanitizer.NormalizeKey(email)));

        public Task<bool> EmailExistsExcluding(string email, Guid excludeCustomerId, CancellationToken ct) =>
            Task.FromResult(_existingEmails.Contains(StringSanitizer.NormalizeKey(email)));
    }
}
