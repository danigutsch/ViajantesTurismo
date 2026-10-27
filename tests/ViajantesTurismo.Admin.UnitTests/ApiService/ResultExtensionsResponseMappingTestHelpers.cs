using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using PandaBear.Results;

namespace ViajantesTurismo.Admin.UnitTests.ApiService;

/// <summary>Helpers for result-to-HTTP response mapping tests.</summary>
internal static class ResultExtensionsResponseMappingTestHelpers
{
    /// <summary>Asserts the result wraps <see cref="HttpValidationProblemDetails" /> and returns those details.</summary>
    public static HttpValidationProblemDetails AssertValidationProblemDetails(ValidationProblem result)
    {
        var valueResult = result.ShouldBeOfType<IValueHttpResult<HttpValidationProblemDetails>>(exactMatch: false);
        return valueResult.Value.ShouldBeOfType<HttpValidationProblemDetails>();
    }

    /// <summary>Creates a failed <see cref="Result" /> through its non-public constructor, bypassing invariants, to test malformed input.</summary>
    public static Result CreateMalformedFailureResult(ResultStatus status, ResultError? error)
    {
        var constructor = typeof(Result).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types: [typeof(ResultStatus), typeof(ResultError)],
            modifiers: null);

        _ = constructor.ShouldNotBeNull();
        return (Result)constructor.Invoke([status, error]);
    }

    /// <summary>Creates a failed <see cref="Result{T}" /> through its non-public constructor, bypassing invariants, to test malformed input.</summary>
    public static Result<T> CreateMalformedFailureResult<T>(ResultStatus status, T? value, ResultError? error)
        where T : notnull
    {
        var constructor = typeof(Result<T>).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types: [typeof(ResultStatus), typeof(T), typeof(ResultError)],
            modifiers: null);

        _ = constructor.ShouldNotBeNull();
        return (Result<T>)constructor.Invoke([status, value, error]);
    }
}
