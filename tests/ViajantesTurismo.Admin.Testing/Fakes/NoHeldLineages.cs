namespace ViajantesTurismo.Admin.Testing.Fakes;

/// <summary>A held-lineage filter for tests that do not exercise holds: nothing is ever held.</summary>
public static class NoHeldLineages
{
    /// <summary>Reports every lineage as unheld.</summary>
    public static Task<IReadOnlySet<Guid>> Filter(IReadOnlyCollection<Guid> lineageIds, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(lineageIds);
        ct.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlySet<Guid>>(new HashSet<Guid>());
    }
}
