using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Shared;

namespace ViajantesTurismo.Admin.UnitTests.Domain;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, PandaBear.Testing.TestTraitValues.SecurityCategory)]
[Trait(PandaBear.Testing.TestTraitNames.ScopeName, "unit")]
public sealed class CustomerPreferenceValidationTests
{
    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    [InlineData(int.MaxValue)]
    public void Physical_info_rejects_undefined_bicycle_selections(int value)
    {
        // Arrange
        var bikeType = (BikeType)value;

        // Act
        var result = PhysicalInfo.Create(75m, 180, bikeType);

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(2, 0)]
    [InlineData(int.MaxValue, 0)]
    [InlineData(0, -1)]
    [InlineData(0, 2)]
    [InlineData(0, int.MaxValue)]
    public void Accommodation_preferences_reject_undefined_room_and_bed_selections(int room, int bed)
    {
        // Arrange
        var roomType = (RoomType)room;
        var bedType = (BedType)bed;

        // Act
        var result = AccommodationPreferences.Create(roomType, bedType, null);

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    [Theory]
    [InlineData(BikeType.None)]
    [InlineData(BikeType.Regular)]
    [InlineData(BikeType.EBike)]
    public void Physical_info_accepts_every_defined_bicycle_selection(BikeType bikeType)
    {
        // Arrange
        const decimal weight = 75m;

        // Act
        var result = PhysicalInfo.Create(weight, 180, bikeType);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.BikeType.ShouldBe(bikeType);
    }

    [Theory]
    [InlineData(RoomType.DoubleOccupancy, BedType.SingleBed)]
    [InlineData(RoomType.DoubleOccupancy, BedType.DoubleBed)]
    [InlineData(RoomType.SingleOccupancy, BedType.SingleBed)]
    [InlineData(RoomType.SingleOccupancy, BedType.DoubleBed)]
    public void Accommodation_preferences_accept_every_defined_room_and_bed_pair(RoomType roomType, BedType bedType)
    {
        // Arrange
        var companion = Guid.CreateVersion7();

        // Act
        var result = AccommodationPreferences.Create(roomType, bedType, companion);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.RoomType.ShouldBe(roomType);
        result.Value.BedType.ShouldBe(bedType);
        result.Value.CompanionId.ShouldBe(companion);
    }
}
