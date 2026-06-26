using static ViajantesTurismo.ArchitectureTests.Conventions.ErrorClassTestsHelpers;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

public sealed class ErrorClassTests
{
    [Fact]
    public void ErrorClasses_Must_Be_Static()
    {
        var errorClasses = GetErrorClasses();

        var violatingTypes = errorClasses
            .Where(type => !type.IsAbstract || !type.IsSealed)
            .ToArray();

        Assert.False(
            violatingTypes.Length != 0,
            $"Expected error classes to be static, but found violations: {string.Join(", ", violatingTypes.Select(t => t.FullName))}");
    }
}
