using System.Reflection;

namespace ViajantesTurismo.Admin.UnitTests.ApiService;

internal static class ResultUnionHelpers
{
    public static object GetInnerResult(object union)
    {
        var resultProperty = union.GetType().GetProperty("Result", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        _ = TestAssert.NotNull(resultProperty);

        var innerResult = resultProperty.GetValue(union);
        _ = TestAssert.NotNull(innerResult);
        return innerResult;
    }
}
