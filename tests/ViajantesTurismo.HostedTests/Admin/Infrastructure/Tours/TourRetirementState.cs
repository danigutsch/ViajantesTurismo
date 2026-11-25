using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Contracts.IntegrationEvents.Tours;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Tours;

/// <summary>Captures the persisted Tour views, retirement evidence and retained booking identifiers.</summary>
/// <param name="Active">The tour in the active view, if visible.</param>
/// <param name="Historical">The tour in the historical view.</param>
/// <param name="ActiveList">Tour summaries visible on the first active page.</param>
/// <param name="Status">The current retirement state, if accepted.</param>
/// <param name="Audits">Audit evidence for the tour.</param>
/// <param name="Events">Retirement events staged for Catalog.</param>
/// <param name="BookingIds">Retained booking identifiers.</param>
internal sealed record TourRetirementState(
    GetTourDto? Active,
    GetTourDto? Historical,
    IReadOnlyList<GetTourListItemDto> ActiveList,
    GetTourRetirementDto? Status,
    TourRetirementAuditRecord[] Audits,
    AdminTourRetiredIntegrationEvent[] Events,
    Guid[] BookingIds);
