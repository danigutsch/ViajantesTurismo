using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace SharedKernel.AspNet.Tests;

/// <summary>
/// Verifies required endpoint metadata validation and endpoint application behavior.
/// </summary>
public sealed class EndpointMetadataTests
{
    [Theory]
    [InlineData(null, "Summary", "Description")]
    [InlineData("", "Summary", "Description")]
    [InlineData("EndpointName", null, "Description")]
    [InlineData("EndpointName", "", "Description")]
    [InlineData("EndpointName", "Summary", null)]
    [InlineData("EndpointName", "Summary", "")]
    public void Constructor_Throws_When_Required_Metadata_Is_Missing(
        string? name,
        string? summary,
        string? description)
    {
        var constructor = typeof(EndpointMetadata).GetConstructor([typeof(string), typeof(string), typeof(string)])
            ?? throw new InvalidOperationException("Could not locate EndpointMetadata constructor.");

        var exception = Assert.Throws<TargetInvocationException>(() => constructor.Invoke([name, summary, description]));

        Assert.IsAssignableFrom<ArgumentException>(exception.InnerException);
    }

    [Fact]
    public void EndpointDefinition_Constructor_Throws_When_Pattern_Is_Missing()
    {
        var metadata = new EndpointMetadata("GetTours", "Retrieves tours.", "Retrieves tours.");

        var exception = Assert.Throws<ArgumentException>(() => new EndpointDefinition(string.Empty, metadata));

        Assert.Equal("pattern", exception.ParamName);
    }

    [Fact]
    public void EndpointDefinition_Constructor_Throws_When_Metadata_Is_Missing()
    {
        var constructor = typeof(EndpointDefinition).GetConstructor([typeof(string), typeof(EndpointMetadata)])
            ?? throw new InvalidOperationException("Could not locate EndpointDefinition constructor.");

        var exception = Assert.Throws<TargetInvocationException>(() => constructor.Invoke(["/", null]));

        Assert.IsAssignableFrom<ArgumentNullException>(exception.InnerException);
    }

    [Fact]
    public async Task WithEndpointMetadata_Applies_Definition_Metadata()
    {
        // Arrange
        var definition = new EndpointDefinition(
            "/{id:guid}",
            new EndpointMetadata(
                "GetTourById",
                "Retrieves a tour.",
                "Retrieves one tour by its identifier."));

        var document = await OpenApiDocumentFactory.CreateDocument("tours", group =>
            group.MapGet(definition.Pattern, (Guid id) => TypedResults.Ok(id))
                .WithEndpointMetadata(definition),
            TestContext.Current.CancellationToken);

        // Act
        Assert.True(document.Paths.TryGetValue("/tours/{id}", out var pathItem));
        Assert.NotNull(pathItem);
        Assert.NotNull(pathItem.Operations);
        Assert.True(pathItem.Operations.TryGetValue(HttpMethod.Get, out var operation));
        Assert.NotNull(operation);

        // Assert
        Assert.Equal("GetTourById", operation.OperationId);
        Assert.Equal("Retrieves a tour.", operation.Summary);
        Assert.Equal("Retrieves one tour by its identifier.", operation.Description);
    }

    [Fact]
    public async Task WithEndpointMetadata_Applies_Metadata_For_All_Supported_Http_Verbs()
    {
        // Arrange
        var getDefinition = new EndpointDefinition(
            "/get",
            new EndpointMetadata("GetSample", "Gets a sample.", "Gets a sample resource."));
        var postDefinition = new EndpointDefinition(
            "/post",
            new EndpointMetadata("CreateSample", "Creates a sample.", "Creates a sample resource."));
        var putDefinition = new EndpointDefinition(
            "/put",
            new EndpointMetadata("UpdateSample", "Updates a sample.", "Updates a sample resource."));
        var patchDefinition = new EndpointDefinition(
            "/patch",
            new EndpointMetadata("PatchSample", "Patches a sample.", "Patches a sample resource."));
        var deleteDefinition = new EndpointDefinition(
            "/delete",
            new EndpointMetadata("DeleteSample", "Deletes a sample.", "Deletes a sample resource."));

        var document = await OpenApiDocumentFactory.CreateDocument("samples", group =>
        {
            group.MapGet(getDefinition.Pattern, () => TypedResults.Ok())
                .WithEndpointMetadata(getDefinition);
            group.MapPost(postDefinition.Pattern, () => TypedResults.Ok())
                .WithEndpointMetadata(postDefinition);
            group.MapPut(putDefinition.Pattern, () => TypedResults.Ok())
                .WithEndpointMetadata(putDefinition);
            group.MapPatch(patchDefinition.Pattern, () => TypedResults.Ok())
                .WithEndpointMetadata(patchDefinition);
            group.MapDelete(deleteDefinition.Pattern, () => TypedResults.Ok())
                .WithEndpointMetadata(deleteDefinition);
        }, TestContext.Current.CancellationToken);

        // Act
        Assert.True(document.Paths.TryGetValue("/samples/get", out var getPath));
        Assert.NotNull(getPath);
        Assert.NotNull(getPath.Operations);
        Assert.True(getPath.Operations.TryGetValue(HttpMethod.Get, out var getOperation));
        Assert.NotNull(getOperation);

        Assert.True(document.Paths.TryGetValue("/samples/post", out var postPath));
        Assert.NotNull(postPath);
        Assert.NotNull(postPath.Operations);
        Assert.True(postPath.Operations.TryGetValue(HttpMethod.Post, out var postOperation));
        Assert.NotNull(postOperation);

        Assert.True(document.Paths.TryGetValue("/samples/put", out var putPath));
        Assert.NotNull(putPath);
        Assert.NotNull(putPath.Operations);
        Assert.True(putPath.Operations.TryGetValue(HttpMethod.Put, out var putOperation));
        Assert.NotNull(putOperation);

        Assert.True(document.Paths.TryGetValue("/samples/patch", out var patchPath));
        Assert.NotNull(patchPath);
        Assert.NotNull(patchPath.Operations);
        Assert.True(patchPath.Operations.TryGetValue(HttpMethod.Patch, out var patchOperation));
        Assert.NotNull(patchOperation);

        Assert.True(document.Paths.TryGetValue("/samples/delete", out var deletePath));
        Assert.NotNull(deletePath);
        Assert.NotNull(deletePath.Operations);
        Assert.True(deletePath.Operations.TryGetValue(HttpMethod.Delete, out var deleteOperation));
        Assert.NotNull(deleteOperation);

        // Assert
        Assert.Equal("GetSample", getOperation.OperationId);
        Assert.Equal("Gets a sample.", getOperation.Summary);
        Assert.Equal("Gets a sample resource.", getOperation.Description);
        Assert.Equal("CreateSample", postOperation.OperationId);
        Assert.Equal("Creates a sample.", postOperation.Summary);
        Assert.Equal("Creates a sample resource.", postOperation.Description);
        Assert.Equal("UpdateSample", putOperation.OperationId);
        Assert.Equal("Updates a sample.", putOperation.Summary);
        Assert.Equal("Updates a sample resource.", putOperation.Description);
        Assert.Equal("PatchSample", patchOperation.OperationId);
        Assert.Equal("Patches a sample.", patchOperation.Summary);
        Assert.Equal("Patches a sample resource.", patchOperation.Description);
        Assert.Equal("DeleteSample", deleteOperation.OperationId);
        Assert.Equal("Deletes a sample.", deleteOperation.Summary);
        Assert.Equal("Deletes a sample resource.", deleteOperation.Description);
    }
}
