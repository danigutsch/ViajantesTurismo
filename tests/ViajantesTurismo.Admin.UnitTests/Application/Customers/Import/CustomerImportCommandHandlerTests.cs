using ViajantesTurismo.Admin.Application.Customers.Import;
using ViajantesTurismo.Admin.Tests.Shared.Fakes;

namespace ViajantesTurismo.Admin.UnitTests.Application.Customers.Import;

public sealed class CustomerImportCommandHandlerTests
{
    [Fact]
    public async Task Handle_With_DryRun_True_Does_Not_Persist_Changes()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var handler = new CustomerImportCommandHandler(unitOfWork);
        var command = new CustomerImportCommand(SuccessCount: 3, ErrorCount: 1, DryRun: true);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(3, result.SuccessCount);
        Assert.Equal(1, result.ErrorCount);
        Assert.Equal(0, unitOfWork.SaveEntitiesCallCount);
    }
}
