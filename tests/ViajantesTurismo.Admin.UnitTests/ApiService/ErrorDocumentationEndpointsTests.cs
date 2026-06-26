using System.Reflection;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ViajantesTurismo.Admin.ApiService;
using ViajantesTurismo.Admin.Contracts;

namespace ViajantesTurismo.Admin.UnitTests.ApiService;

public sealed class ErrorDocumentationEndpointsTests
{
    private static readonly Type EndpointsType = typeof(ResultExtensions).Assembly
        .GetType("ViajantesTurismo.Admin.ApiService.Errors.ErrorDocumentationEndpoints")
        ?? throw new InvalidOperationException("Could not locate ErrorDocumentationEndpoints type.");

    [Fact]
    public void GetAllErrorDocumentation_Returns_All_Entries()
    {
        var method = EndpointsType.GetMethod("GetAllErrorDocumentation", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method);

        var result = method.Invoke(null, []);
        var ok = Assert.IsType<Ok<IReadOnlyList<GetErrorDocumentationDto>>>(result);
        Assert.NotNull(ok.Value);
        Assert.NotEmpty(ok.Value);
    }

    [Fact]
    public void GetErrorDocumentationByIdentifier_Returns_NotFound_For_Unknown_Identifier()
    {
        var method = EndpointsType.GetMethod("GetErrorDocumentationByIdentifier", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method);

        var result = method.Invoke(null, ["missing-entry"]);
        var union = Assert.IsType<Results<Ok<GetErrorDocumentationDto>, NotFound<ProblemDetails>>>(result);
        var notFound = Assert.IsType<NotFound<ProblemDetails>>(ResultUnionHelpers.GetInnerResult(union));
        Assert.NotNull(notFound.Value);
        Assert.Equal("Error Documentation Not Found", notFound.Value.Title);
    }

    [Fact]
    public void GetErrorDocumentationByIdentifier_Returns_Entry_For_Known_Identifier()
    {
        var entries = typeof(ResultExtensions).Assembly
            .GetType("ViajantesTurismo.Admin.ApiService.Errors.ErrorDocumentationCatalog")?
            .GetMethod("GetEntries", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?
            .Invoke(null, []) as IReadOnlyList<GetErrorDocumentationDto>;
        Assert.NotNull(entries);

        var knownIdentifier = Assert.Single(entries, static entry =>
            string.Equals(entry.ProviderType, "ViajantesTurismo.Admin.Domain.Tours.TourErrors", StringComparison.Ordinal)
            && string.Equals(entry.MemberName, "TourNotFound", StringComparison.Ordinal)).Identifier;

        var method = EndpointsType.GetMethod("GetErrorDocumentationByIdentifier", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method);

        var result = method.Invoke(null, [knownIdentifier]);
        var union = Assert.IsType<Results<Ok<GetErrorDocumentationDto>, NotFound<ProblemDetails>>>(result);
        var ok = Assert.IsType<Ok<GetErrorDocumentationDto>>(ResultUnionHelpers.GetInnerResult(union));
        Assert.NotNull(ok.Value);
        Assert.Equal(knownIdentifier, ok.Value.Identifier);
    }

}
