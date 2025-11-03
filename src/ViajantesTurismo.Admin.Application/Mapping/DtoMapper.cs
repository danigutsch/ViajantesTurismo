using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.AdminApi.Contracts;

namespace ViajantesTurismo.Admin.Application.Mapping;

/// <summary>
/// Maps domain entities to DTOs for read-side queries.
/// This mapper exists in Infrastructure layer to avoid circular dependencies with ApiService.
/// </summary>
public static class DtoMapper
{
    /// <summary>
    /// Maps a <see cref="BikeType"/> to a <see cref="BikeTypeDto"/>.
    /// </summary>
    public static BikeTypeDto MapToBikeTypeDto(BikeType bikeType)
    {
        return bikeType switch
        {
            BikeType.None => BikeTypeDto.None,
            BikeType.Regular => BikeTypeDto.Regular,
            BikeType.EBike => BikeTypeDto.EBike,
            _ => throw new ArgumentOutOfRangeException(nameof(bikeType), bikeType, "Invalid bike type value.")
        };
    }
}
