namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>An active tour choice displayed in an Admin form.</summary>
/// <param name="Id">The tour's unique ID.</param>
/// <param name="Name">The display name of the tour.</param>
/// <param name="Identifier">The tour's operational identifier.</param>
/// <param name="StartDate">The tour's scheduled start date.</param>
public sealed record GetTourOptionDto(Guid Id, string Name, string Identifier, DateOnly StartDate);
