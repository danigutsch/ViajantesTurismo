using ViajantesTurismo.Admin.Application.Customers.Import;
using ViajantesTurismo.Admin.Tests.Shared.Fakes;

namespace ViajantesTurismo.Admin.UnitTests.Application.Customers.Import;

public sealed class CustomerImportCommandHandlerTests
{
    private readonly FakeUnitOfWork _uow;
    private readonly FakeCustomerStore _fakeCustomerStore;
    private readonly CustomerImportCommandHandler _sut;

    private const string CsvHeaders =
        "FirstName,LastName,Gender,BirthDate,Nationality,Occupation,NationalId,IdNationality," +
        "Email,Mobile,Street,Neighborhood,PostalCode,City,State,Country," +
        "WeightKg,HeightCentimeters,BikeType,RoomType,BedType,EmergencyContactName,EmergencyContactMobile";

    private static string BuildCsv(string email = "test@example.com") =>
        $"{CsvHeaders}\nJohn,Doe,Male,1990-01-01,USA,Engineer,A12345678,USA," +
        $"{email},+1234567890,123 Main St,Downtown,10001,New York,NY,USA," +
        $"75,175,Regular,DoubleOccupancy,SingleBed,Jane Doe,+0987654321";

    public CustomerImportCommandHandlerTests()
    {
        _uow = new FakeUnitOfWork();
        _fakeCustomerStore = new FakeCustomerStore();
        _sut = new CustomerImportCommandHandler(_fakeCustomerStore, _uow, TimeProvider.System);
    }

    [Fact]
    public async Task Handle_With_DryRun_True_Does_Not_Persist_Changes()
    {
        // Arrange
        var command = new CustomerImportCommand(BuildCsv(), DryRun: true);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(0, result.ErrorCount);
        Assert.Equal(0, _uow.SaveEntitiesCallCount);
    }

    [Fact]
    public async Task Handle_With_DryRun_False_Persists_New_Customer()
    {
        // Arrange
        var command = new CustomerImportCommand(BuildCsv("imported@example.com"), DryRun: false);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(0, result.ErrorCount);
        Assert.Equal(1, _uow.SaveEntitiesCallCount);
        Assert.True(await _fakeCustomerStore.EmailExists("imported@example.com", CancellationToken.None));
    }

    [Fact]
    public async Task Handle_With_Duplicate_Email_In_File_Counts_Second_Row_As_Error()
    {
        // Arrange
        const string duplicateEmail = "dup@example.com";
        const string csv =
            $"{CsvHeaders}\n" +
            $"John,Doe,Male,1990-01-01,USA,Engineer,A12345678,USA,{duplicateEmail},+1234567890,123 Main St,Downtown,10001,New York,NY,USA,75,175,Regular,DoubleOccupancy,SingleBed,Jane Doe,+0987654321\n" +
            $"Jane,Smith,Female,1992-06-15,USA,Doctor,B87654321,USA,{duplicateEmail},+9876543210,456 Oak Ave,Uptown,20002,Boston,MA,USA,65,165,Regular,SingleOccupancy,DoubleBed,Jim Smith,+1122334455";

        var command = new CustomerImportCommand(csv, DryRun: false);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(1, result.ErrorCount);
    }

    [Fact]
    public async Task Handle_With_Email_Already_In_Database_Skips_Row_And_Counts_As_Error()
    {
        // Arrange
        const string existingEmail = "existing@example.com";
        var storeWithExisting = new FakeCustomerStore(seededEmails: [existingEmail]);
        var sut = new CustomerImportCommandHandler(storeWithExisting, _uow, TimeProvider.System);

        var command = new CustomerImportCommand(BuildCsv(existingEmail), DryRun: false);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.ErrorCount);
        Assert.Equal(0, _uow.SaveEntitiesCallCount);
    }
}

