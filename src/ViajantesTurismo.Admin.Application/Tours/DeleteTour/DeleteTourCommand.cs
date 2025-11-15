namespace ViajantesTurismo.Admin.Application.Tours.DeleteTour;

/// <summary>
/// Command to delete a tour.
/// </summary>
public sealed record DeleteTourCommand(Guid TourId);
