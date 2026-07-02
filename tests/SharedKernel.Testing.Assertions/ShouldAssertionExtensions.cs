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

    /// <summary>
    /// Verifies that a collection contains the expected item.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="actual">The actual collection.</param>
    /// <param name="expected">The expected item.</param>
    public static void ShouldContain<T>(this IEnumerable<T> actual, T expected) => TestAssert.Contains(expected, actual);

    /// <summary>
    /// Verifies that a collection is empty.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="actual">The actual collection.</param>
    public static void ShouldBeEmpty<T>(this IEnumerable<T> actual) => TestAssert.Empty(actual);

    /// <summary>
    /// Verifies that a value has the expected runtime type.
    /// </summary>
    /// <typeparam name="T">The expected type.</typeparam>
    /// <param name="actual">The actual value.</param>
    /// <returns>The typed value.</returns>
    public static T ShouldBeOfType<T>(this object? actual) => TestAssert.IsType<T>(actual);

    /// <summary>
    /// Verifies that an action throws the expected exception.
    /// </summary>
    /// <typeparam name="T">The expected exception type.</typeparam>
    /// <param name="action">The action.</param>
    /// <returns>The thrown exception.</returns>
    public static T ShouldThrow<T>(this Action action)
        where T : Exception => TestAssert.Throws<T>(action);

    /// <summary>
    /// Verifies that an async action throws the expected exception.
    /// </summary>
    /// <typeparam name="T">The expected exception type.</typeparam>
    /// <param name="action">The async action.</param>
    /// <returns>The thrown exception.</returns>
    public static Task<T> ShouldThrow<T>(this Func<Task> action)
        where T : Exception => TestAssert.Throws<T>(action);

    /// <summary>
    /// Verifies that a reflection invocation throws an inner exception of the expected type.
    /// </summary>
    /// <typeparam name="T">The expected inner exception type.</typeparam>
    /// <param name="action">The reflection action.</param>
    /// <returns>The typed inner exception.</returns>
    public static T ShouldThrowInner<T>(this Action action)
        where T : Exception => ExceptionAssertions.ThrowsInner<T>(action);
}
