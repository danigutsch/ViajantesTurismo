using SharedKernel.Testing.Assertions;
using Xunit;

namespace ViajantesTurismo.Admin.ContractTests.Bookings;

/// <summary>
/// Verifies that the generated bookings artifact remains compatible with the canonical artifact.
/// </summary>
public sealed class BookingsOpenApiGeneratedArtifactCompatibilityTests
{
    [Fact]
    public async Task Generated_bookings_artifact_matches_the_canonical_artifact_contract_slice()
    {
        // Arrange
        var generatedContract = await BookingsOpenApiDocumentClient.GetGeneratedContract(TestContext.Current.CancellationToken);
        var canonicalContract = await BookingsOpenApiDocumentClient.GetContract(TestContext.Current.CancellationToken);

        // Assert
        var nonNullGeneratedContract = generatedContract.ShouldNotBeNull();
        BookingsOpenApiAssertions.MatchesGeneratedArtifact(canonicalContract, nonNullGeneratedContract);
    }
}
