using PandaBear.Results;
using ViajantesTurismo.Admin.Application.Customers;
using ViajantesTurismo.Admin.Application.Customers.CreateCustomer;
using ViajantesTurismo.Admin.Testing.Builders;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.UnitTests.Application.Customers;

public sealed class CreateCustomerCommandHandlerTests
{
    [Fact]
    public async Task Handle_translates_a_persistence_email_conflict()
    {
        // Arrange
        var dto = DtoBuilders.BuildCreateCustomerDto(email: "traveler@example.com");
        var command = new CreateCustomerCommand(
            dto.PersonalInfo,
            dto.IdentificationInfo,
            dto.ContactInfo,
            dto.Address,
            dto.PhysicalInfo,
            dto.AccommodationPreferences,
            dto.EmergencyContact,
            dto.MedicalInfo);
        var handler = new CreateCustomerCommandHandler(
            new FakeCustomerStore(),
            new ThrowingUnitOfWork(new CustomerEmailConflictException()),
            TimeProvider.System);

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Status.ShouldBe(ResultStatus.Conflict);
    }

    [Fact]
    public async Task Handle_returns_invalid_without_creating_a_customer_when_address_or_physical_info_is_invalid()
    {
        // Arrange
        var dto = DtoBuilders.BuildCreateCustomerDto(email: "invalid-details@example.com");
        var command = new CreateCustomerCommand(
            dto.PersonalInfo,
            dto.IdentificationInfo,
            dto.ContactInfo,
            dto.Address with
            {
                Street = ""
            },
            dto.PhysicalInfo with
            {
                WeightKg = 0m
            },
            dto.AccommodationPreferences,
            dto.EmergencyContact with
            {
                Name = ""
            },
            dto.MedicalInfo);
        var store = new FakeCustomerStore();
        var handler = new CreateCustomerCommandHandler(store, new FakeUnitOfWork(), TimeProvider.System);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Status.ShouldBe(ResultStatus.Invalid);
        store.AllCustomers.ShouldBeEmpty();
    }
}
