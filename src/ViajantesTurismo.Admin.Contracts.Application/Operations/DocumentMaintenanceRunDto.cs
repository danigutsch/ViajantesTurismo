namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Counts-only evidence recorded when a document maintenance batch completed or was refused.</summary>
/// <param name="OccurredAtUtc">When the evidence was recorded.</param>
/// <param name="PurgedCount">The number of records the batch removed.</param>
/// <param name="HeldCount">The number of records protected by a hold.</param>
/// <param name="Outcome">The recorded outcome of the batch.</param>
public sealed record DocumentMaintenanceRunDto(DateTime OccurredAtUtc, int PurgedCount, int HeldCount, string Outcome);
