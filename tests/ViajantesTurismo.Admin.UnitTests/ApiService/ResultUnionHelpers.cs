using System.Reflection;

namespace ViajantesTurismo.Admin.UnitTests.ApiService;

/// <summary>Reflection helpers for result union types in tests.</summary>
internal static class ResultUnionHelpers
{
    /// <summary>Reads the non-null inner <c>Result</c> property value of a union instance.</summary>
    public static object GetInnerResult(object union)
    {
        var resultProperty = union.GetType().GetProperty("Result", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        _ = resultProperty.ShouldNotBeNull();

        var innerResult = resultProperty.GetValue(union);
        _ = innerResult.ShouldNotBeNull();
        return innerResult;
    }
}
