using ViajantesTurismo.Admin.Application.Customers.Import;
using ViajantesTurismo.Admin.Tests.Shared.Fakes;

namespace ViajantesTurismo.Admin.UnitTests.Application.Customers.Import;

public sealed class CustomerImportWorkflowServiceTests
{
    private const string CsvHeaders =
        "FirstName,LastName,Gender,BirthDate,Nationality,Occupation,NationalId,IdNationality," +
        "Email,Mobile,Street,Neighborhood,PostalCode,City,State,Country," +
        "WeightKg,HeightCentimeters,BikeType,RoomType,BedType,EmergencyContactName,EmergencyContactMobile";

    [Fact]
    public async Task ImportAsync_With_FileAndDatabaseDuplicates_Reports_Conflicts_Using_Configured_Normalization_Rules()
    {
        // Arrange
        const string firstEmail = "jose.silva@example.com";
        const string nameDuplicateEmail = "jose.silva.2@example.com";
        const string dbDuplicateEmail = "existing@example.com";

        var csv =
            $"""
             {CsvHeaders}
             {BuildRow("José", "Silva", firstEmail)}
             {BuildRow("Jose", "Silva", nameDuplicateEmail)}
             {BuildRow("Maria", "Souza", dbDuplicateEmail)}
             """;

        var store = new FakeCustomerStore([dbDuplicateEmail]);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new CustomerImportCommandHandler(store, unitOfWork, TimeProvider.System);
        var sut = new CustomerImportWorkflowService(store, handler);

        // Act
        var result = await sut.Import(csv, CancellationToken.None);

        // Assert
        Assert.NotNull(result.Conflicts);
        Assert.Equal(2, result.Conflicts.Count);
        Assert.Contains(result.Conflicts, c => c.Email.Equals(nameDuplicateEmail, StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Conflicts, c => c.Email.Equals(dbDuplicateEmail, StringComparison.OrdinalIgnoreCase));
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(0, result.ErrorCount);
    }

    private static string BuildRow(string firstName, string lastName, string email)
    {
        return $"{firstName},{lastName},Male,1990-01-01,Brazilian,Engineer,A12345678,BR," +
               $"{email},+5511999999999,Rua A,Centro,01000-000,São Paulo,SP,Brazil," +
               "75,175,Regular,DoubleOccupancy,SingleBed,Emergency Name,+5511888888888";
    }
}
