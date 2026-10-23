using static ViajantesTurismo.ArchitectureTests.Conventions.ErrorClassTestsHelpers;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

public sealed class ErrorClassTests
{
    [Fact]
    public void ErrorClasses_must_be_static()
    {
        var errorClasses = GetErrorClasses();

        var violatingTypes = errorClasses
            .Where(type => !type.IsAbstract || !type.IsSealed)
            .ToArray();

        var areDifferent = violatingTypes.Length != 0;
        areDifferent.ShouldBeFalse(
            $"Expected error classes to be static, but found violations: {string.Join(", ", violatingTypes.Select(t => t.FullName))}");
    }
}
