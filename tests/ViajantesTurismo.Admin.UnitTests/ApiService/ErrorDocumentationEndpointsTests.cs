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
        _ = (method).ShouldNotBeNull();

        var result = method.Invoke(null, []);
        var ok = (result).ShouldBeOfType<Ok<IReadOnlyList<GetErrorDocumentationDto>>>();
        (ok.Value).ShouldNotBeNull();
        (ok.Value).ShouldNotBeEmpty();
    }

    [Fact]
    public void GetErrorDocumentationByIdentifier_returns_notfound_for_unknown_identifier()
    {
        var method = EndpointsType.GetMethod("GetErrorDocumentationByIdentifier", BindingFlags.Static | BindingFlags.NonPublic);
        _ = (method).ShouldNotBeNull();

        var result = method.Invoke(null, ["missing-entry"]);
        var union = (result).ShouldBeOfType<Results<Ok<GetErrorDocumentationDto>, NotFound<ProblemDetails>>>();
        var notFound = (ResultUnionHelpers.GetInnerResult(union)).ShouldBeOfType<NotFound<ProblemDetails>>();
        (notFound.Value).ShouldNotBeNull();
        (notFound.Value.Title).ShouldBe("Error Documentation Not Found");
    }

    [Fact]
    public void GetErrorDocumentationByIdentifier_returns_entry_for_known_identifier()
    {
        var entries = typeof(ResultExtensions).Assembly
            .GetType("ViajantesTurismo.Admin.ApiService.Errors.ErrorDocumentationCatalog")?
            .GetMethod("GetEntries", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?
            .Invoke(null, []) as IReadOnlyList<GetErrorDocumentationDto>;
        _ = (entries).ShouldNotBeNull();

        var knownIdentifier = (entries).ShouldHaveSingleItem(static entry =>
            string.Equals(entry.ProviderType, "ViajantesTurismo.Admin.Domain.Tours.TourErrors", StringComparison.Ordinal)
            && string.Equals(entry.MemberName, "TourNotFound", StringComparison.Ordinal)).Identifier;

        var method = EndpointsType.GetMethod("GetErrorDocumentationByIdentifier", BindingFlags.Static | BindingFlags.NonPublic);
        _ = (method).ShouldNotBeNull();

        var result = method.Invoke(null, [knownIdentifier]);
        var union = (result).ShouldBeOfType<Results<Ok<GetErrorDocumentationDto>, NotFound<ProblemDetails>>>();
        var ok = (ResultUnionHelpers.GetInnerResult(union)).ShouldBeOfType<Ok<GetErrorDocumentationDto>>();
        (ok.Value).ShouldNotBeNull();
        (ok.Value.Identifier).ShouldBe(knownIdentifier);
    }

}
