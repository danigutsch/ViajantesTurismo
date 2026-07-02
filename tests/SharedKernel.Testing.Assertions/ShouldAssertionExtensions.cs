using System.Diagnostics.CodeAnalysis;

namespace SharedKernel.Testing.Assertions;

/// <summary>
/// Provides assertion helpers for values used in tests.
/// </summary>
public static class ShouldAssertionExtensions
{
    /// <summary>
    /// Verifies equality with the expected value.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="actual">The actual value.</param>
    /// <param name="expected">The expected value.</param>
    public static void ShouldBe<T>(this T actual, T expected) => TestAssert.Equal(expected, actual);

    /// <summary>
    /// Verifies reference equality with the expected value.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="actual">The actual value.</param>
    /// <param name="expected">The expected reference.</param>
    public static void ShouldBeSameAs<T>(this T actual, T expected)
        where T : class => TestAssert.Same(expected, actual);

    /// <summary>
    /// Verifies that a nullable reference is not null and updates compiler null-state flow.
    /// </summary>
    /// <typeparam name="T">The reference type.</typeparam>
    /// <param name="actual">The actual value.</param>
    /// <returns>The non-null value.</returns>
    public static T ShouldNotBeNull<T>([NotNull] this T? actual)
        where T : class => TestAssert.NotNull(actual);

    /// <summary>
    /// Verifies that a nullable value type is not null.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="actual">The actual value.</param>
    /// <returns>The non-null value.</returns>
    public static T ShouldNotBeNull<T>(this T? actual)
        where T : struct => TestAssert.NotNull(actual);

    /// <summary>
    /// Verifies that a string contains the expected fragment with ordinal comparison.
    /// </summary>
    /// <param name="actual">The actual value.</param>
    /// <param name="expected">The expected fragment.</param>
    public static void ShouldContain(this string? actual, string expected) => TestAssert.Contains(expected, actual, StringComparison.Ordinal);
}
