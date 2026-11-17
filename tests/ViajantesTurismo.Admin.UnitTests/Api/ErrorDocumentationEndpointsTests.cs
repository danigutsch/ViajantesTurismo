using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ViajantesTurismo.Admin.Api;
using ViajantesTurismo.Admin.Contracts.Http;

namespace ViajantesTurismo.Admin.UnitTests.Api;

public sealed class ErrorDocumentationEndpointsTests
{
    private static readonly Type EndpointsType = typeof(ResultExtensions).Assembly
        .GetType("ViajantesTurismo.Admin.Api.Errors.ErrorDocumentationEndpoints")
        ?? throw new InvalidOperationException("Could not locate ErrorDocumentationEndpoints type.");

    [Fact]
    public void GetAllErrorDocumentation_returns_all_entries()
    {
        // Arrange
        var method = EndpointsType.GetMethod("GetAllErrorDocumentation", BindingFlags.Static | BindingFlags.NonPublic);
        _ = method.ShouldNotBeNull();

        // Act
        var result = method.Invoke(null, [new DefaultHttpContext()]);

        // Assert
        var union = result.ShouldBeOfType<Results<Ok<IReadOnlyList<GetErrorDocumentationDto>>, StatusCodeHttpResult>>();
        var ok = ResultUnionHelpers.GetInnerResult(union).ShouldBeOfType<Ok<IReadOnlyList<GetErrorDocumentationDto>>>();
        ok.Value.ShouldNotBeNull();
        ok.Value.ShouldNotBeEmpty();
    }

    [Theory]
    [InlineData("<script>alert(1)</script>", "Error documentation entry was not found.")]
    [InlineData(" ", "Error documentation entry identifier was not provided.")]
    public void GetErrorDocumentationByIdentifier_returns_notfound_with_fixed_detail_that_does_not_echo_the_identifier(string identifier, string expectedDetail)
    {
        // Arrange
        var method = EndpointsType.GetMethod("GetErrorDocumentationByIdentifier", BindingFlags.Static | BindingFlags.NonPublic);
        _ = method.ShouldNotBeNull();

        // Act
        var result = method.Invoke(null, [new DefaultHttpContext(), identifier]);

        // Assert
        var union = result.ShouldBeOfType<Results<Ok<GetErrorDocumentationDto>, NotFound<ProblemDetails>, StatusCodeHttpResult>>();
        var notFound = ResultUnionHelpers.GetInnerResult(union).ShouldBeOfType<NotFound<ProblemDetails>>();
        notFound.Value.ShouldNotBeNull();
        notFound.Value.Title.ShouldBe("Error Documentation Not Found");
        notFound.Value.Detail.ShouldBe(expectedDetail);
    }

    [Fact]
    public void GetErrorDocumentationByIdentifier_returns_entry_for_known_identifier()
    {
        // Arrange
        var entries = typeof(ResultExtensions).Assembly
            .GetType("ViajantesTurismo.Admin.Api.Errors.ErrorDocumentationCatalog")?
            .GetMethod("GetEntries", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?
            .Invoke(null, []) as IReadOnlyList<GetErrorDocumentationDto>;
        _ = entries.ShouldNotBeNull();

        var knownIdentifier = entries.ShouldHaveSingleItem(static entry =>
            string.Equals(entry.ProviderType, "ViajantesTurismo.Admin.Domain.Tours.TourErrors", StringComparison.Ordinal)
            && string.Equals(entry.MemberName, "TourNotFound", StringComparison.Ordinal)).Identifier;

        var method = EndpointsType.GetMethod("GetErrorDocumentationByIdentifier", BindingFlags.Static | BindingFlags.NonPublic);
        _ = method.ShouldNotBeNull();

        // Act
        var result = method.Invoke(null, [new DefaultHttpContext(), knownIdentifier]);

        // Assert
        var union = result.ShouldBeOfType<Results<Ok<GetErrorDocumentationDto>, NotFound<ProblemDetails>, StatusCodeHttpResult>>();
        var ok = ResultUnionHelpers.GetInnerResult(union).ShouldBeOfType<Ok<GetErrorDocumentationDto>>();
        ok.Value.ShouldNotBeNull();
        ok.Value.Identifier.ShouldBe(knownIdentifier);
    }

}
