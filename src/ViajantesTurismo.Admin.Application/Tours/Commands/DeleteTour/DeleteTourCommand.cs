namespace ViajantesTurismo.Admin.Application.Tours.Commands.DeleteTour;

/// <summary>
/// Command to delete a tour.
/// </summary>
public sealed record DeleteTourCommand(Guid TourId);
