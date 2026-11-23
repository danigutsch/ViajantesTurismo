namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Text and capacity for a bounded customer option search.</summary>
/// <param name="Query">The customer name to match, or a complete customer identifier for exact lookup.</param>
/// <param name="Limit">The requested number of options.</param>
/// <param name="ExcludedCustomerId">An optional customer that cannot be selected in the calling form.</param>
public sealed record CustomerOptionSearchDto(string? Query, int Limit, Guid? ExcludedCustomerId = null);
