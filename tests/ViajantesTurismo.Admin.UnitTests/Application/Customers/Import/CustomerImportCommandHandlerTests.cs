using ViajantesTurismo.Admin.Application.Customers.Import;
using ViajantesTurismo.Admin.Tests.Shared.Behavior;
using ViajantesTurismo.Admin.Tests.Shared.Fakes;

namespace ViajantesTurismo.Admin.UnitTests.Application.Customers.Import;

public sealed class CustomerImportCommandHandlerTests
{
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
        var customer = EntityBuilders.BuildCustomer(email: "imported@example.com");
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
        var existingCustomer = EntityBuilders.BuildCustomer(email: "before@example.com");
        var incomingCustomer = EntityBuilders.BuildCustomer(email: "after@example.com");
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
