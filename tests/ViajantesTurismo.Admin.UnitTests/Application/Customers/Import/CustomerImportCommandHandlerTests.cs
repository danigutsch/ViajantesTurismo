using ViajantesTurismo.Admin.Application.Customers.Import;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Tests.Shared.Fakes;

namespace ViajantesTurismo.Admin.UnitTests.Application.Customers.Import;

public sealed class CustomerImportCommandHandlerTests
{
    private static Customer CreateTestCustomer(TimeProvider timeProvider, string email = "imported@example.com")
    {
        var personal = PersonalInfo.Create("John", "Doe", "Male", DateTime.UtcNow.AddYears(-30), "USA", "Engineer", timeProvider).Value;
        var identification = IdentificationInfo.Create("ID123", "USA").Value;
        var contact = ContactInfo.Create(email, "+1000000000", null, null).Value;
        var address = Address.Create("Street", "Comp", "Neighborhood", "12345", "City", "State", "Country").Value;
        var physical = PhysicalInfo.Create(70m, 180, BikeType.Regular).Value;
        var accommodation = AccommodationPreferences.Create(RoomType.DoubleOccupancy, BedType.SingleBed, null).Value;
        var emergency = EmergencyContact.Create("Jane Doe", "+1000000001").Value;
        var medical = MedicalInfo.Create("None", null).Value;
        return new Customer(personal, identification, contact, address, physical, accommodation, emergency, medical);
    }

    [Fact]
    public async Task Handle_With_DryRun_True_Does_Not_Persist_Changes()
    {
        // Arrange
        var customerStore = new FakeCustomerStore();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new CustomerImportCommandHandler(customerStore, unitOfWork);
        var command = new CustomerImportCommand(3, 1, true);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(3, result.SuccessCount);
        Assert.Equal(1, result.ErrorCount);
        Assert.Equal(0, unitOfWork.SaveEntitiesCallCount);
    }

    [Fact]
    public async Task Handle_With_DryRun_False_Persists_New_Customer()
    {
        // Arrange
        var customerStore = new FakeCustomerStore();
        var unitOfWork = new FakeUnitOfWork();
        var customer = CreateTestCustomer(TimeProvider.System);
        var handler = new CustomerImportCommandHandler(customerStore, unitOfWork);
        var command = new CustomerImportCommand(1, 0, false, [customer]);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(0, result.ErrorCount);
        Assert.Equal(1, unitOfWork.SaveEntitiesCallCount);
        var persistedCustomer = await customerStore.GetById(customer.Id, CancellationToken.None);
        Assert.NotNull(persistedCustomer);
    }

    [Fact]
    public async Task Handle_With_Overwrite_Updates_Existing_Customer()
    {
        // Arrange
        var customerStore = new FakeCustomerStore();
        var unitOfWork = new FakeUnitOfWork();
        var existingCustomer = CreateTestCustomer(TimeProvider.System, "before@example.com");
        var incomingCustomer = CreateTestCustomer(TimeProvider.System, "after@example.com");
        customerStore.Seed(existingCustomer);

        var handler = new CustomerImportCommandHandler(customerStore, unitOfWork);
        var command = new CustomerImportCommand(
            1,
            0,
            false,
            [],
            [new CustomerOverwritePair(existingCustomer, incomingCustomer)]);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(0, result.ErrorCount);
        Assert.Equal(1, unitOfWork.SaveEntitiesCallCount);
        Assert.Equal("after@example.com", existingCustomer.ContactInfo.Email);
    }
}
