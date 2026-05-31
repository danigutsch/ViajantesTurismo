using Xunit;

namespace ViajantesTurismo.Admin.ContractTests.Bookings;

/// <summary>
/// Verifies that the canonical bookings OpenAPI artifact preserves the consumer-owned contract slice.
/// </summary>
public sealed class BookingsOpenApiCanonicalArtifactTests
{
    [Fact]
    public async Task Reads_The_Bookings_OpenApi_Document_Through_The_Canonical_Artifact()
    {
        // Act
        var contract = await BookingsOpenApiDocumentClient.GetContract(TestContext.Current.CancellationToken);

        // Assert
        BookingsOpenApiAssertions.MatchesCanonicalConsumerSlice(contract);
    }
}
