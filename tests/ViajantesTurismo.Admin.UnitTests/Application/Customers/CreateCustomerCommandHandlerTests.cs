using SharedKernel.Results;
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
}
