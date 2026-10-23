using ViajantesTurismo.Admin.Testing.Fakes.ApiClients;

namespace ViajantesTurismo.Admin.ContractTests.ApiClients;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, Infrastructure.TestTraits.ContractCategory)]
public sealed class FakeAdminApiClientParityTests
{
    [Fact]
    public async Task FakeToursApiClient_propagates_configured_programmer_exceptions()
    {
        // Arrange
        var expected = new InvalidOperationException("programmer fault");
        var sut = new FakeToursApiClient();
        sut.SetGetToursException(expected);

        // Act
        var exception = await ShouldAssertionExtensions.ShouldThrow<InvalidOperationException>((Func<Task>)(async () =>
            await sut.GetTours(TestContext.Current.CancellationToken)));

        // Assert
        exception.ShouldBeSameAs(expected);
    }

    [Fact]
    public async Task FakeCustomersApiClient_propagates_configured_programmer_exceptions()
    {
        // Arrange
        var expected = new InvalidOperationException("programmer fault");
        var sut = new FakeCustomersApiClient();
        sut.SetGetCustomersException(expected);

        // Act
        var exception = await ShouldAssertionExtensions.ShouldThrow<InvalidOperationException>((Func<Task>)(async () =>
            await sut.GetCustomers(TestContext.Current.CancellationToken)));

        // Assert
        exception.ShouldBeSameAs(expected);
    }

    [Fact]
    public async Task FakeBookingsApiClient_propagates_configured_programmer_exceptions()
    {
        // Arrange
        var expected = new InvalidOperationException("programmer fault");
        var sut = new FakeBookingsApiClient();
        sut.SetGetAllBookingsException(expected);

        // Act
        var exception = await ShouldAssertionExtensions.ShouldThrow<InvalidOperationException>((Func<Task>)(async () =>
            await sut.GetAllBookings(TestContext.Current.CancellationToken)));

        // Assert
        exception.ShouldBeSameAs(expected);
    }

    [Fact]
    public async Task FakeToursApiClient_returns_an_explicit_transport_outcome()
    {
        // Arrange
        var expected = FakeApiClientOutcomes.TransportFailure<Contracts.Application.GetTourDto[]>();
        var sut = new FakeToursApiClient();
        sut.SetGetToursOutcome(expected);

        // Act
        var outcome = await sut.GetTours(TestContext.Current.CancellationToken);

        // Assert
        outcome.ShouldBeSameAs(expected);
    }

    [Fact]
    public async Task FakeCustomersApiClient_returns_an_explicit_transport_outcome()
    {
        // Arrange
        var expected = FakeApiClientOutcomes.TransportFailure<Contracts.Application.GetCustomerDto[]>();
        var sut = new FakeCustomersApiClient();
        sut.SetGetCustomersOutcome(expected);

        // Act
        var outcome = await sut.GetCustomers(TestContext.Current.CancellationToken);

        // Assert
        outcome.ShouldBeSameAs(expected);
    }

    [Fact]
    public async Task FakeBookingsApiClient_returns_an_explicit_transport_outcome()
    {
        // Arrange
        var expected = FakeApiClientOutcomes.TransportFailure<Contracts.Application.GetBookingDto[]>();
        var sut = new FakeBookingsApiClient();
        sut.SetGetAllBookingsOutcome(expected);

        // Act
        var outcome = await sut.GetAllBookings(TestContext.Current.CancellationToken);

        // Assert
        outcome.ShouldBeSameAs(expected);
    }

    [Fact]
    public async Task FakeToursApiClient_propagates_caller_cancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var sut = new FakeToursApiClient();

        // Act
        var exception = await ShouldAssertionExtensions.ShouldThrowAssignableTo<OperationCanceledException>((Func<Task>)(async () =>
            await sut.GetTours(cts.Token)));

        // Assert
        exception.CancellationToken.ShouldBe(cts.Token);
    }

    [Fact]
    public async Task FakeCustomersApiClient_propagates_caller_cancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var sut = new FakeCustomersApiClient();

        // Act
        var exception = await ShouldAssertionExtensions.ShouldThrowAssignableTo<OperationCanceledException>((Func<Task>)(async () =>
            await sut.GetCustomers(cts.Token)));

        // Assert
        exception.CancellationToken.ShouldBe(cts.Token);
    }

    [Fact]
    public async Task FakeBookingsApiClient_propagates_caller_cancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var sut = new FakeBookingsApiClient();

        // Act
        var exception = await ShouldAssertionExtensions.ShouldThrowAssignableTo<OperationCanceledException>((Func<Task>)(async () =>
            await sut.GetAllBookings(cts.Token)));

        // Assert
        exception.CancellationToken.ShouldBe(cts.Token);
    }
}
