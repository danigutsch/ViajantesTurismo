namespace SharedKernel.Mediator.Testing.ReferenceDispatcher;

/// <summary>
/// Marks the test-only reference-dispatcher assembly.
/// </summary>
internal static class ReferenceDispatcherAssemblyMarker
{
    /// <summary>
    /// Gets the reference-dispatcher assembly name.
    /// </summary>
    public static string AssemblyName { get; } = typeof(ReferenceDispatcherAssemblyMarker).Assembly.GetName().Name!;
}
