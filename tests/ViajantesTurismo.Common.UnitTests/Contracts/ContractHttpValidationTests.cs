using System.Net;
using System.Text.Json;
using ViajantesTurismo.Common.Contracts;

namespace ViajantesTurismo.Common.UnitTests.Contracts;

public sealed class ContractHttpValidationTests
{
    [Fact]
    public async Task Ensure_success_returns_when_response_is_successful()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.NoContent);

        await ContractHttpValidation.EnsureSuccessOrThrowValidationException(
            response,
            ContractHttpValidationTestsJsonContext.Default.ContractValidationProblemDto,
            TestContext.Current.CancellationToken);

        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Ensure_success_throws_http_exception_for_non_validation_failures()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.NotFound);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            ContractHttpValidation.EnsureSuccessOrThrowValidationException(
                response,
                ContractHttpValidationTestsJsonContext.Default.ContractValidationProblemDto,
                TestContext.Current.CancellationToken));

        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
    }

    [Fact]
    public async Task Ensure_success_throws_validation_exception_with_errors_for_validation_problem()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("""
                {
                  "errors": {
                    "Name": ["Name is required.", "Name is too short."],
                    "Email": ["Email is invalid."]
                  }
                }
                """)
        };

        var exception = await Assert.ThrowsAsync<ContractValidationException>(() =>
            ContractHttpValidation.EnsureSuccessOrThrowValidationException(
                response,
                ContractHttpValidationTestsJsonContext.Default.ContractValidationProblemDto,
                TestContext.Current.CancellationToken));

        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Equal(["Name is required.", "Name is too short."], exception.ValidationErrors["Name"]);
        Assert.Equal(["Email is invalid."], exception.ValidationErrors["Email"]);
        Assert.Contains("Name is required.", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Email is invalid.", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Ensure_success_preserves_json_exception_for_malformed_validation_problem()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("{ not json")
        };

        var exception = await Assert.ThrowsAsync<ContractValidationException>(() =>
            ContractHttpValidation.EnsureSuccessOrThrowValidationException(
                response,
                ContractHttpValidationTestsJsonContext.Default.ContractValidationProblemDto,
                TestContext.Current.CancellationToken));

        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.IsType<JsonException>(exception.InnerException);
        Assert.Empty(exception.ValidationErrors);
        Assert.Equal("Validation problem response body was malformed.", exception.Message);
    }

    [Fact]
    public async Task Ensure_success_throws_validation_exception_when_validation_problem_has_no_errors()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("""
                {
                  "title": "One or more validation errors occurred."
                }
                """)
        };

        var exception = await Assert.ThrowsAsync<ContractValidationException>(() =>
            ContractHttpValidation.EnsureSuccessOrThrowValidationException(
                response,
                ContractHttpValidationTestsJsonContext.Default.ContractValidationProblemDto,
                TestContext.Current.CancellationToken));

        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Empty(exception.ValidationErrors);
        Assert.Equal("Validation problem response body did not contain errors.", exception.Message);
    }
}
