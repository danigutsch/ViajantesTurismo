namespace SharedKernel.Mediator.Testing.ReferenceDispatcher;

/// <summary>
/// Marks the test-only reference-dispatcher assembly until the implementation arrives in the next slice.
/// </summary>
internal static class ReferenceDispatcherAssemblyMarker
{
    /// <summary>
    /// Gets the reference-dispatcher assembly name.
    /// </summary>
    public static string AssemblyName { get; } = typeof(ReferenceDispatcherAssemblyMarker).Assembly.GetName().Name!;
}
