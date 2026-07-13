using ViajantesTurismo.Admin.Domain.Shared;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps.Validation;

[Binding]
[Scope(Feature = "Accommodation Preferences Validation")]
public sealed class AccommodationPreferencesValidationSteps(CustomerContext context)
{
    [When(@"I create accommodation preferences with double room, double bed, and companion ID (\d+)")]
    public void WhenICreateAccommodationPreferencesWithDoubleRoomDoubleBedAndCompanionId(int companionId)
    {
        context.AccommodationPreferences = AccommodationPreferences.Create(
            RoomType.DoubleOccupancy,
            BedType.DoubleBed,
            CreateDeterministicCompanionId(companionId));
    }

    [When("I create accommodation preferences with single room, single bed, and no companion")]
    public void WhenICreateAccommodationPreferencesWithSingleRoomSingleBedAndNoCompanion()
    {
        context.AccommodationPreferences = AccommodationPreferences.Create(RoomType.SingleOccupancy, BedType.SingleBed, null);
    }

    [When("I create accommodation preferences with single room, double bed, and no companion")]
    public void WhenICreateAccommodationPreferencesWithSingleRoomDoubleBedAndNoCompanion()
    {
        context.AccommodationPreferences = AccommodationPreferences.Create(RoomType.SingleOccupancy, BedType.DoubleBed, null);
    }

    [When("I create accommodation preferences with double room, double bed, and no companion")]
    public void WhenICreateAccommodationPreferencesWithDoubleRoomDoubleBedAndNoCompanion()
    {
        context.AccommodationPreferences = AccommodationPreferences.Create(RoomType.DoubleOccupancy, BedType.DoubleBed, null);
    }

    [When(@"I create accommodation preferences with single room, single bed, and companion ID (\d+)")]
    public void WhenICreateAccommodationPreferencesWithSingleRoomSingleBedAndCompanionId(int companionId)
    {
        context.AccommodationPreferences = AccommodationPreferences.Create(
            RoomType.SingleOccupancy,
            BedType.SingleBed,
            CreateDeterministicCompanionId(companionId));
    }

    [Then("the accommodation preferences should be created successfully")]
    public void ThenTheAccommodationPreferencesShouldBeCreatedSuccessfully()
    {
        context.AccommodationPreferences.ShouldNotBeNull();
    }

    [Then(@"the companion ID should be (\d+)")]
    public void ThenTheCompanionIdShouldBe(int expectedCompanionId)
    {
        var accommodationPreferences = context.AccommodationPreferences.ShouldNotBeNull();
        accommodationPreferences.CompanionId.ShouldBe(CreateDeterministicCompanionId(expectedCompanionId));
    }

    [Then("the companion ID should be null")]
    public void ThenTheCompanionIdShouldBeNull()
    {
        var accommodationPreferences = context.AccommodationPreferences.ShouldNotBeNull();
        accommodationPreferences.CompanionId.ShouldBeNull();
    }

    private static Guid CreateDeterministicCompanionId(int companionId)
    {
        return new Guid(companionId, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
    }
}
