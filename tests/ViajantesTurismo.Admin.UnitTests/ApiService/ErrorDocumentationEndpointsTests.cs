using System.Reflection;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ViajantesTurismo.Admin.ApiService;
using ViajantesTurismo.Admin.Contracts.Http;

namespace ViajantesTurismo.Admin.UnitTests.ApiService;

public sealed class ErrorDocumentationEndpointsTests
{
    private static readonly Type EndpointsType = typeof(ResultExtensions).Assembly
        .GetType("ViajantesTurismo.Admin.ApiService.Errors.ErrorDocumentationEndpoints")
        ?? throw new InvalidOperationException("Could not locate ErrorDocumentationEndpoints type.");

    [Fact]
    public void GetAllErrorDocumentation_returns_all_entries()
    {
        var method = EndpointsType.GetMethod("GetAllErrorDocumentation", BindingFlags.Static | BindingFlags.NonPublic);
        _ = TestAssert.NotNull(method);

        var result = method.Invoke(null, []);
        var ok = TestAssert.IsType<Ok<IReadOnlyList<GetErrorDocumentationDto>>>(result);
        TestAssert.NotNull(ok.Value);
        TestAssert.NotEmpty(ok.Value);
    }

    [Fact]
    public void GetErrorDocumentationByIdentifier_returns_notfound_for_unknown_identifier()
    {
        var method = EndpointsType.GetMethod("GetErrorDocumentationByIdentifier", BindingFlags.Static | BindingFlags.NonPublic);
        _ = TestAssert.NotNull(method);

        var result = method.Invoke(null, ["missing-entry"]);
        var union = TestAssert.IsType<Results<Ok<GetErrorDocumentationDto>, NotFound<ProblemDetails>>>(result);
        var notFound = TestAssert.IsType<NotFound<ProblemDetails>>(ResultUnionHelpers.GetInnerResult(union));
        TestAssert.NotNull(notFound.Value);
        TestAssert.Equal("Error Documentation Not Found", notFound.Value.Title);
    }

    [Fact]
    public void GetErrorDocumentationByIdentifier_returns_entry_for_known_identifier()
    {
        var entries = typeof(ResultExtensions).Assembly
            .GetType("ViajantesTurismo.Admin.ApiService.Errors.ErrorDocumentationCatalog")?
            .GetMethod("GetEntries", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?
            .Invoke(null, []) as IReadOnlyList<GetErrorDocumentationDto>;
        _ = TestAssert.NotNull(entries);

        var knownIdentifier = TestAssert.ExactlyOne(entries, static entry =>
            string.Equals(entry.ProviderType, "ViajantesTurismo.Admin.Domain.Tours.TourErrors", StringComparison.Ordinal)
            && string.Equals(entry.MemberName, "TourNotFound", StringComparison.Ordinal)).Identifier;

        var method = EndpointsType.GetMethod("GetErrorDocumentationByIdentifier", BindingFlags.Static | BindingFlags.NonPublic);
        _ = TestAssert.NotNull(method);

        var result = method.Invoke(null, [knownIdentifier]);
        var union = TestAssert.IsType<Results<Ok<GetErrorDocumentationDto>, NotFound<ProblemDetails>>>(result);
        var ok = TestAssert.IsType<Ok<GetErrorDocumentationDto>>(ResultUnionHelpers.GetInnerResult(union));
        TestAssert.NotNull(ok.Value);
        TestAssert.Equal(knownIdentifier, ok.Value.Identifier);
    }

}
