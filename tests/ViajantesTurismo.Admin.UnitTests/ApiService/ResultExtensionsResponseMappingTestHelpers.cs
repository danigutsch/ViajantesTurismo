using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using SharedKernel.Results;

namespace ViajantesTurismo.Admin.UnitTests.ApiService;

internal static class ResultExtensionsResponseMappingTestHelpers
{
    public static HttpValidationProblemDetails AssertValidationProblemDetails(ValidationProblem result)
    {
        var valueResult = Assert.IsType<IValueHttpResult<HttpValidationProblemDetails>>(result, exactMatch: false);
        return Assert.IsType<HttpValidationProblemDetails>(valueResult.Value);
    }

    public static Result CreateMalformedFailureResult(ResultStatus status, ResultError? error)
    {
        var constructor = typeof(Result).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types: [typeof(ResultStatus), typeof(ResultError)],
            modifiers: null);

        Assert.NotNull(constructor);
        return (Result)constructor.Invoke([status, error]);
    }

    public static Result<T> CreateMalformedFailureResult<T>(ResultStatus status, T? value, ResultError? error)
        where T : notnull
    {
        var constructor = typeof(Result<T>).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types: [typeof(ResultStatus), typeof(T), typeof(ResultError)],
            modifiers: null);

        Assert.NotNull(constructor);
        return (Result<T>)constructor.Invoke([status, value, error]);
    }
}
