using Xunit;

namespace ViajantesTurismo.Admin.ContractTests.Bookings;

/// <summary>
/// Verifies that the canonical bookings OpenAPI artifact preserves the consumer-owned contract slice.
/// </summary>
public sealed class BookingsOpenApiCanonicalArtifactTests
{
    [Fact]
    public async Task Reads_the_bookings_openApi_document_through_the_canonical_artifact()
    {
        // Act
        var contract = await BookingsOpenApiDocumentClient.GetContract(TestContext.Current.CancellationToken);

        // Assert
        BookingsOpenApiAssertions.MatchesCanonicalConsumerSlice(contract);
    }
}
