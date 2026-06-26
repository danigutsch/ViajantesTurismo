using System.Reflection;
using SharedKernel.Results;

namespace ViajantesTurismo.Common.UnitTests.Results;

internal static class ResultConvertErrorMalformedStatusTestsHelpers
{
    public static Result CreateMalformedResult(ResultStatus status, ResultError? error)
    {
        var constructor = typeof(Result).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types: [typeof(ResultStatus), typeof(ResultError)],
            modifiers: null);

        Assert.NotNull(constructor);
        return (Result)constructor.Invoke([status, error]);
    }

    public static Result<T> CreateMalformedGenericResult<T>(ResultStatus status, T value, ResultError? error)
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
