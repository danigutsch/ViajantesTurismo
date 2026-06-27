using ViajantesTurismo.Admin.Application.Customers.Import;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.UnitTests.Application.Customers.Import;

public sealed class CustomerImportCommandHandlerTests
{
    private readonly FakeUnitOfWork _uow;
    private readonly FakeCustomerStore _fakeCustomerStore;
    private readonly CustomerImportCommandHandler _sut;

    public CustomerImportCommandHandlerTests()
    {
        _uow = new FakeUnitOfWork();
        _fakeCustomerStore = new FakeCustomerStore();
        _sut = new CustomerImportCommandHandler(_fakeCustomerStore, _uow, TimeProvider.System);
    }

    [Fact]
    public async Task Handle_with_dryRun_true_does_not_persist_changes()
    {
        // Arrange
        var command = new CustomerImportCommand(CsvRows.Build(), DryRun: true);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(0, result.ErrorCount);
        Assert.Equal(0, _uow.SaveEntitiesCallCount);
    }

    [Fact]
    public async Task Handle_with_dryRun_false_persists_new_customer()
    {
        // Arrange
        var command = new CustomerImportCommand(CsvRows.Build("imported@example.com"), DryRun: false);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(0, result.ErrorCount);
        Assert.Equal(1, _uow.SaveEntitiesCallCount);
        Assert.True(await _fakeCustomerStore.EmailExists("imported@example.com", CancellationToken.None));
    }

    [Fact]
    public async Task Handle_with_duplicate_email_in_file_counts_second_row_as_error()
    {
        // Arrange
        const string duplicateEmail = "dup@example.com";
        const string csv =
            $"{CsvRows.Headers}\n" +
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
    public async Task Handle_with_email_already_in_database_skips_row_and_counts_as_error()
    {
        // Arrange
        const string existingEmail = "existing@example.com";
        var storeWithExisting = new FakeCustomerStore(seededEmails: [existingEmail]);
        var sut = new CustomerImportCommandHandler(storeWithExisting, _uow, TimeProvider.System);

        var command = new CustomerImportCommand(CsvRows.Build(existingEmail), DryRun: false);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.ErrorCount);
        Assert.Equal(0, _uow.SaveEntitiesCallCount);
    }

}
