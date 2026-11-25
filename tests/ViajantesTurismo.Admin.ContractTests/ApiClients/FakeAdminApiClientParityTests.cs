using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Testing.Fakes.ApiClients;

namespace ViajantesTurismo.Admin.ContractTests.ApiClients;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, Infrastructure.TestTraits.ContractCategory)]
public sealed class FakeAdminApiClientParityTests
{
    [Fact]
    public async Task FakeToursApiClient_propagates_configured_programmer_exceptions()
    {
        // Arrange
        var expected = new InvalidOperationException("programmer fault");
        var sut = new FakeToursApiClient();
        sut.SetGetTourByIdException(expected);

        // Act
        var exception = await ShouldAssertionExtensions.ShouldThrow<InvalidOperationException>(async () =>
            await sut.GetTourById(Guid.CreateVersion7(), TestContext.Current.CancellationToken));

        // Assert
        exception.ShouldBeSameAs(expected);
    }

    [Fact]
    public async Task FakeCustomersApiClient_propagates_configured_programmer_exceptions()
    {
        // Arrange
        var expected = new InvalidOperationException("programmer fault");
        var sut = new FakeCustomersApiClient();
        sut.SetGetCustomerByIdException(expected);

        // Act
        var exception = await ShouldAssertionExtensions.ShouldThrow<InvalidOperationException>(async () =>
            await sut.GetCustomerById(Guid.CreateVersion7(), TestContext.Current.CancellationToken));

        // Assert
        exception.ShouldBeSameAs(expected);
    }

    [Fact]
    public async Task FakeBookingsApiClient_propagates_configured_programmer_exceptions()
    {
        // Arrange
        var expected = new InvalidOperationException("programmer fault");
        var sut = new FakeBookingsApiClient();
        sut.SetGetBookingPageException(expected);

        // Act
        var exception = await ShouldAssertionExtensions.ShouldThrow<InvalidOperationException>(async () =>
            await sut.GetBookingPage(1, BookingPageSort.Default, descending: false, TestContext.Current.CancellationToken));

        // Assert
        exception.ShouldBeSameAs(expected);
    }

    [Fact]
    public async Task FakeToursApiClient_returns_an_explicit_transport_outcome()
    {
        // Arrange
        var expected = FakeApiClientOutcomes.TransportFailure<GetTourDto>();
        var sut = new FakeToursApiClient();
        sut.SetGetTourByIdOutcome(expected);

        // Act
        var outcome = await sut.GetTourById(Guid.CreateVersion7(), TestContext.Current.CancellationToken);

        // Assert
        outcome.ShouldBeSameAs(expected);
    }

    [Fact]
    public async Task FakeCustomersApiClient_returns_an_explicit_transport_outcome()
    {
        // Arrange
        var expected = FakeApiClientOutcomes.TransportFailure<CustomerDetailsDto>();
        var sut = new FakeCustomersApiClient();
        sut.SetGetCustomerByIdOutcome(expected);

        // Act
        var outcome = await sut.GetCustomerById(Guid.CreateVersion7(), TestContext.Current.CancellationToken);

        // Assert
        outcome.ShouldBeSameAs(expected);
    }

    [Fact]
    public async Task FakeBookingsApiClient_returns_an_explicit_transport_outcome()
    {
        // Arrange
        var expected = FakeApiClientOutcomes.TransportFailure<GetBookingPageDto>();
        var sut = new FakeBookingsApiClient();
        sut.SetGetBookingPageOutcome(expected);

        // Act
        var outcome = await sut.GetBookingPage(1, BookingPageSort.Default, descending: false, TestContext.Current.CancellationToken);

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
        var exception = await ShouldAssertionExtensions.ShouldThrowAssignableTo<OperationCanceledException>(async () =>
            await sut.GetTourById(Guid.CreateVersion7(), cts.Token));

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
        var exception = await ShouldAssertionExtensions.ShouldThrowAssignableTo<OperationCanceledException>(async () =>
            await sut.GetCustomerById(Guid.CreateVersion7(), cts.Token));

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
        var exception = await ShouldAssertionExtensions.ShouldThrowAssignableTo<OperationCanceledException>(async () =>
            await sut.GetBookingPage(1, BookingPageSort.Default, descending: false, cts.Token));

        // Assert
        exception.CancellationToken.ShouldBe(cts.Token);
    }
}
