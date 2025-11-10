using Reqnroll;
using ViajantesTurismo.Admin.BehaviorTests.Context;
using ViajantesTurismo.Admin.Domain.Customers;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps;

[Binding]
[Scope(Feature = "Accommodation Preferences Validation")]
public sealed class AccommodationPreferencesValidationSteps(AccommodationPreferencesContext context)
{
    [When(@"I create accommodation preferences with double room, double bed, and companion ID (\d+)")]
    public void WhenICreateAccommodationPreferencesWithDoubleRoomDoubleBedAndCompanionId(int companionId)
    {
        context.RoomType = RoomType.DoubleRoom;
        context.BedType = BedType.DoubleBed;
        context.CompanionId = companionId;
        context.Result = AccommodationPreferences.Create(RoomType.DoubleRoom, BedType.DoubleBed, companionId);
        if (context.Result.IsSuccess)
        {
            context.AccommodationPreferences = context.Result.Value;
        }
    }

    [When("I create accommodation preferences with single room, single bed, and no companion")]
    public void WhenICreateAccommodationPreferencesWithSingleRoomSingleBedAndNoCompanion()
    {
        context.RoomType = RoomType.SingleRoom;
        context.BedType = BedType.SingleBed;
        context.CompanionId = null;
        context.Result = AccommodationPreferences.Create(RoomType.SingleRoom, BedType.SingleBed, null);
        if (context.Result.IsSuccess)
        {
            context.AccommodationPreferences = context.Result.Value;
        }
    }

    [When("I create accommodation preferences with single room, double bed, and no companion")]
    public void WhenICreateAccommodationPreferencesWithSingleRoomDoubleBedAndNoCompanion()
    {
        context.RoomType = RoomType.SingleRoom;
        context.BedType = BedType.DoubleBed;
        context.CompanionId = null;
        context.Result = AccommodationPreferences.Create(RoomType.SingleRoom, BedType.DoubleBed, null);
        if (context.Result.IsSuccess)
        {
            context.AccommodationPreferences = context.Result.Value;
        }
    }

    [When("I create accommodation preferences with double room, double bed, and no companion")]
    public void WhenICreateAccommodationPreferencesWithDoubleRoomDoubleBedAndNoCompanion()
    {
        context.RoomType = RoomType.DoubleRoom;
        context.BedType = BedType.DoubleBed;
        context.CompanionId = null;
        context.Result = AccommodationPreferences.Create(RoomType.DoubleRoom, BedType.DoubleBed, null);
        if (context.Result.IsSuccess)
        {
            context.AccommodationPreferences = context.Result.Value;
        }
    }

    [When(@"I create accommodation preferences with single room, single bed, and companion ID (\d+)")]
    public void WhenICreateAccommodationPreferencesWithSingleRoomSingleBedAndCompanionId(int companionId)
    {
        context.RoomType = RoomType.SingleRoom;
        context.BedType = BedType.SingleBed;
        context.CompanionId = companionId;
        context.Result = AccommodationPreferences.Create(RoomType.SingleRoom, BedType.SingleBed, companionId);
        if (context.Result.IsSuccess)
        {
            context.AccommodationPreferences = context.Result.Value;
        }
    }

    [Then("the accommodation preferences should be created successfully")]
    public void ThenTheAccommodationPreferencesShouldBeCreatedSuccessfully()
    {
        Assert.True(context.Result.IsSuccess);
        Assert.NotNull(context.AccommodationPreferences);
    }

    [Then(@"the companion ID should be (\d+)")]
    public void ThenTheCompanionIdShouldBe(int expectedCompanionId)
    {
        Assert.NotNull(context.AccommodationPreferences);
        Assert.Equal(expectedCompanionId, context.AccommodationPreferences.CompanionId);
    }

    [Then("the companion ID should be null")]
    public void ThenTheCompanionIdShouldBeNull()
    {
        Assert.NotNull(context.AccommodationPreferences);
        Assert.Null(context.AccommodationPreferences.CompanionId);
    }
}
