using Microsoft.OpenApi;
using Xunit;

namespace ViajantesTurismo.Admin.ContractTests;

/// <summary>
/// Verifies that mapped Admin API endpoints expose required operation metadata.
/// </summary>
public sealed class AdminEndpointMetadataContractTests
{
    [Fact]
    public async Task All_Admin_OpenApi_Operations_Have_Required_Metadata()
    {
        // Arrange
        var document = await AdminOpenApiDocumentFactory.CreateDocument(
            "v1",
            TestContext.Current.CancellationToken,
            "MapToursEndpoints",
            "MapCustomerEndpoints",
            "MapCustomerImportEndpoints",
            "MapBookingEndpoints",
            "MapErrorDocumentationEndpoints");

        // Act
        var operationsMissingMetadata = document.Paths
            .SelectMany(path => GetOperationMetadataFailures(path.Key, path.Value))
            .ToArray();

        // Assert
        Assert.Empty(operationsMissingMetadata);
    }

    private static IEnumerable<string> GetOperationMetadataFailures(string path, IOpenApiPathItem pathItem)
    {
        if (pathItem.Operations is null)
        {
            yield return $"{path}: missing operations.";
            yield break;
        }

        foreach (var operation in pathItem.Operations)
        {
            var operationLabel = $"{operation.Key} {path}";

            if (string.IsNullOrWhiteSpace(operation.Value.OperationId))
            {
                yield return $"{operationLabel}: missing operationId.";
            }

            if (string.IsNullOrWhiteSpace(operation.Value.Summary))
            {
                yield return $"{operationLabel}: missing summary.";
            }

            if (string.IsNullOrWhiteSpace(operation.Value.Description))
            {
                yield return $"{operationLabel}: missing description.";
            }
        }
    }
}
