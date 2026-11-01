using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.UnitTests.ApiService;

namespace ViajantesTurismo.Admin.UnitTests.Application.Documents;

public sealed class DocumentSourceVersionReaderTests
{
    [Fact]
    public async Task Read_reports_a_missing_booking_as_not_found()
    {
        // Arrange
        var reader = new DocumentSourceVersionReader(new FakeQueryService(null));

        // Act
        var result = await reader.Read(Guid.CreateVersion7(), TestContext.Current.CancellationToken);

        // Assert
        result.Result.Status.ShouldBe(PandaBear.Results.ResultStatus.NotFound);
        result.MissingSource.ShouldBe(DocumentSourceKind.Booking);
    }

    [Fact]
    public async Task Read_reports_a_missing_tour_as_not_found()
    {
        // Arrange
        var bookingId = Guid.CreateVersion7();
        var booking = DocumentSourceTestData.Booking(bookingId, Guid.CreateVersion7());
        var reader = new DocumentSourceVersionReader(new FakeQueryService(booking));

        // Act
        var result = await reader.Read(bookingId, TestContext.Current.CancellationToken);

        // Assert
        result.Result.Status.ShouldBe(PandaBear.Results.ResultStatus.NotFound);
        result.MissingSource.ShouldBe(DocumentSourceKind.Tour);
    }

    [Fact]
    public async Task Read_returns_the_source_version_of_the_current_booking_and_tour()
    {
        // Arrange
        var bookingId = Guid.CreateVersion7();
        var tourId = Guid.CreateVersion7();
        var booking = DocumentSourceTestData.Booking(bookingId, tourId);
        var tour = DocumentSourceTestData.Tour(tourId);
        var reader = new DocumentSourceVersionReader(new FakeQueryService(booking, tour));

        // Act
        var result = await reader.Read(bookingId, TestContext.Current.CancellationToken);

        // Assert
        result.Result.IsSuccess.ShouldBeTrue();
        result.MissingSource.ShouldBeNull();
        result.Result.Value.ShouldBe(ContractDocumentDraftFactory.CreateSourceVersion(booking, tour));
    }

    [Fact]
    public async Task Read_returns_a_different_version_when_the_booking_changes()
    {
        // Arrange
        var bookingId = Guid.CreateVersion7();
        var tourId = Guid.CreateVersion7();
        var tour = DocumentSourceTestData.Tour(tourId);
        var original = await new DocumentSourceVersionReader(
                new FakeQueryService(DocumentSourceTestData.Booking(bookingId, tourId), tour))
            .Read(bookingId, TestContext.Current.CancellationToken);
        var changed = await new DocumentSourceVersionReader(
                new FakeQueryService(DocumentSourceTestData.Booking(bookingId, tourId, totalPrice: 1500m), tour))
            .Read(bookingId, TestContext.Current.CancellationToken);

        // Assert
        changed.Result.Value.ShouldNotBe(original.Result.Value);
    }
}
