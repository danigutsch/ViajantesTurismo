using System.Diagnostics;
using PandaBear.Testing.Snapshots;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.ApiServiceTests.Tours;

/// <summary>Holds one synthetic attempt's assertions and the observations permitted in its manifest.</summary>
/// <param name="TourId">The test-owned tour ID used in assertions.</param>
/// <param name="RetirementStatus">The retirement request's HTTP status.</param>
/// <param name="QueryStatus">The retirement status request's HTTP status.</param>
/// <param name="Retirement">The retirement state returned for a successful status request.</param>
/// <param name="QueryNoStore">Whether the returned retirement state forbids caching.</param>
/// <param name="IsRetired">Whether the synthetic tour was retired.</param>
/// <param name="Saves">The number of unit-of-work saves.</param>
/// <param name="BookingStatus">The synthetic booking's status when present.</param>
/// <param name="AttemptId">The opaque manifest attempt ID.</param>
/// <param name="ObservedTrace">The server trace ID when observed.</param>
/// <param name="StartedUtc">The attempt start time in UTC.</param>
/// <param name="DurationMs">The duration in milliseconds.</param>
internal sealed record TourRetirementReproductionRun(
    Guid TourId,
    HttpStatusCode RetirementStatus,
    HttpStatusCode QueryStatus,
    GetTourRetirementDto? Retirement,
    bool QueryNoStore,
    bool IsRetired,
    int Saves,
    BookingStatus? BookingStatus,
    Guid AttemptId,
    ActivityTraceId? ObservedTrace,
    DateTimeOffset StartedUtc,
    int DurationMs)
{
    /// <summary>Creates a manifest for the outcome the test has verified.</summary>
    internal ReproductionManifest Manifest(ReproductionSpecification specification, ReproductionOutcome outcome) =>
        new(specification.Revision, specification.Scenario, AttemptId,
            ObservedTrace is { } trace ? [trace] : [], outcome, StartedUtc, DurationMs);
}
