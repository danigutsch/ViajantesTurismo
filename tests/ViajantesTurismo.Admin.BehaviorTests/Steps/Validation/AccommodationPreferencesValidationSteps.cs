using Reqnroll;
using ViajantesTurismo.Admin.BehaviorTests.Context;
using ViajantesTurismo.Admin.Domain.Customers;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps.Validation;

[Binding]
[Scope(Feature = "Accommodation Preferences Validation")]
public sealed class AccommodationPreferencesValidationSteps(CustomerContext context)
{
    [When(@"I create accommodation preferences with double room, double bed, and companion ID (\d+)")]
    public void WhenICreateAccommodationPreferencesWithDoubleRoomDoubleBedAndCompanionId(int companionId)
    {
        context.CompanionId = Guid.CreateVersion7();
        context.AccommodationPreferencesResult = AccommodationPreferences.Create(RoomType.DoubleOccupancy, BedType.DoubleBed, context.CompanionId);
    }

    [When("I create accommodation preferences with single room, single bed, and no companion")]
    public void WhenICreateAccommodationPreferencesWithSingleRoomSingleBedAndNoCompanion()
    {
        context.AccommodationPreferencesResult = AccommodationPreferences.Create(RoomType.SingleOccupancy, BedType.SingleBed, null);
    }

    [When("I create accommodation preferences with single room, double bed, and no companion")]
    public void WhenICreateAccommodationPreferencesWithSingleRoomDoubleBedAndNoCompanion()
    {
        context.AccommodationPreferencesResult = AccommodationPreferences.Create(RoomType.SingleOccupancy, BedType.DoubleBed, null);
    }

    [When("I create accommodation preferences with double room, double bed, and no companion")]
    public void WhenICreateAccommodationPreferencesWithDoubleRoomDoubleBedAndNoCompanion()
    {
        context.AccommodationPreferencesResult = AccommodationPreferences.Create(RoomType.DoubleOccupancy, BedType.DoubleBed, null);
    }

    [When(@"I create accommodation preferences with single room, single bed, and companion ID (\d+)")]
    public void WhenICreateAccommodationPreferencesWithSingleRoomSingleBedAndCompanionId(int companionId)
    {
        context.CompanionId = Guid.CreateVersion7();
        context.AccommodationPreferencesResult = AccommodationPreferences.Create(RoomType.SingleOccupancy, BedType.SingleBed, context.CompanionId);
    }

    [Then("the accommodation preferences should be created successfully")]
    public void ThenTheAccommodationPreferencesShouldBeCreatedSuccessfully()
    {
        Assert.NotNull(context.AccommodationPreferencesResult);
        Assert.True(context.AccommodationPreferencesResult.Value.IsSuccess);
    }

    [Then(@"the companion ID should be (\d+)")]
    public void ThenTheCompanionIdShouldBe(int expectedCompanionId)
    {
        Assert.NotNull(context.AccommodationPreferencesResult);
        Assert.Equal(context.CompanionId, context.AccommodationPreferencesResult.Value.Value.CompanionId);
    }

    [Then("the companion ID should be null")]
    public void ThenTheCompanionIdShouldBeNull()
    {
        Assert.NotNull(context.AccommodationPreferencesResult);
        Assert.Null(context.AccommodationPreferencesResult.Value.Value.CompanionId);
    }
}
