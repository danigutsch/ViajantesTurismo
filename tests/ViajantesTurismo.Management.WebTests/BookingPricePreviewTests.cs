using PandaBear.Testing;
using ViajantesTurismo.Management.Web.Helpers;

namespace ViajantesTurismo.Management.WebTests;

/// <summary>Protects live form previews independently from server-side booking acceptance.</summary>
[TestScope(TestScope.Unit)]
public sealed class BookingPricePreviewTests
{
    [Fact]
    public void Captures_room_both_bikes_and_discount_as_one_immutable_display_value()
    {
        // Arrange
        var tour = BuildTourDto(price: 100, singleRoomSupplementPrice: 10, regularBikePrice: 20, eBikePrice: 30);
        var selection = new BookingFormModel
        {
            RoomType = RoomTypeDto.SingleOccupancy,
            PrincipalBikeType = BikeTypeDto.Regular,
            CompanionBikeType = BikeTypeDto.EBike,
            DiscountType = DiscountTypeDto.Percentage,
            DiscountAmount = 12.5m
        };

        // Act
        var preview = BookingPricePreview.Create(tour, selection);
        selection.DiscountAmount = 50;

        // Assert
        preview.Subtotal.ShouldBe(160m);
        preview.DiscountAmount.ShouldBe(20m);
        preview.TotalPrice.ShouldBe(140m);
    }

    [Fact]
    public void Clamps_an_excessive_draft_discount_for_display_without_claiming_booking_acceptance()
    {
        // Arrange
        var tour = BuildTourDto(price: 100);
        var selection = new BookingFormModel { DiscountType = DiscountTypeDto.Absolute, DiscountAmount = 101 };

        // Act
        var preview = BookingPricePreview.Create(tour, selection);

        // Assert
        preview.Subtotal.ShouldBe(100m);
        preview.DiscountAmount.ShouldBe(101m);
        preview.TotalPrice.ShouldBe(0m);
    }

    [Theory]
    [InlineData(DiscountTypeDto.None, 100)]
    [InlineData((DiscountTypeDto)999, 100)]
    [InlineData(DiscountTypeDto.Percentage, -10)]
    public void Leaves_incomplete_or_invalid_form_options_to_server_validation(DiscountTypeDto type, decimal amount)
    {
        // Arrange
        var tour = BuildTourDto(price: 100);
        var selection = new BookingFormModel
        {
            PrincipalBikeType = (BikeTypeDto)999,
            DiscountType = type,
            DiscountAmount = amount
        };

        // Act
        var preview = BookingPricePreview.Create(tour, selection);

        // Assert
        preview.TotalPrice.ShouldBe(100m);
        preview.DiscountAmount.ShouldBe(0m);
    }
}
