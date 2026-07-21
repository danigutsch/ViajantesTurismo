using Npgsql;
using SharedKernel.Testing;

namespace ViajantesTurismo.Admin.IntegrationTests.Infrastructure.Documents;

[Trait(TestTraitNames.ScopeName, TestTraits.DatabaseIntegrationScope)]
[Trait(SharedKernelTestTraitNames.CapabilityName, AdminTestTraitValues.GeneratedDocumentsCapability)]
public sealed class BookingCancellationAtDocumentPersistenceScenarioTests(ApiFixture fixture)
{
    [Fact]
    public async Task Create_disposes_data_source_when_booking_lookup_throws()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var database = await fixture.CreateIsolatedPostgreSqlDatabase(cancellationToken);
        await using var dataSource = database.CreateDataSource("booking-cancellation-create-failure");

        // Act
        var lookupFailure = await BookingCancellationAtDocumentPersistenceScenarioTestHelpers.CaptureCreateFailure(
            dataSource,
            cancellationToken);
        var openFailure = await BookingCancellationAtDocumentPersistenceScenarioTestHelpers.CaptureOpenFailure(
            dataSource,
            cancellationToken);

        // Assert
        lookupFailure.ShouldNotBeNull().SqlState.ShouldBe(PostgresErrorCodes.UndefinedTable);
        openFailure.ShouldNotBeNull();
    }
}
