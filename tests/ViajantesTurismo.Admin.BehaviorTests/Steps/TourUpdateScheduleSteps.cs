using System.Globalization;
using Reqnroll;
using ViajantesTurismo.Admin.BehaviorTests.Context;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps;

[Binding]
public sealed class TourUpdateScheduleSteps(TourContext tourContext)
{
    [Given(@"a tour exists with dates from ""(.*)"" to ""(.*)""")]
    public void GivenATourExistsWithDatesFromTo(string startDateString, string endDateString)
    {
        var startDate = DateTime.Parse(startDateString, CultureInfo.InvariantCulture).ToUniversalTime();
        var endDate = DateTime.Parse(endDateString, CultureInfo.InvariantCulture).ToUniversalTime();
        tourContext.Tour = EntityBuilders.BuildTour(startDate: startDate, endDate: endDate);
    }

    [When(@"I try to update the tour schedule to start ""(.*)"" and end ""(.*)""")]
    public void WhenITryToUpdateTheTourScheduleToStartAndEnd(string startDateString, string endDateString)
    {
        var startDate = DateTime.Parse(startDateString, CultureInfo.InvariantCulture).ToUniversalTime();
        var endDate = DateTime.Parse(endDateString, CultureInfo.InvariantCulture).ToUniversalTime();
        tourContext.UpdateResult = tourContext.Tour.UpdateSchedule(startDate, endDate);
    }

    [When(@"I update the tour schedule to start ""(.*)"" and end ""(.*)""")]
    public void WhenIUpdateTheTourScheduleToStartAndEnd(string startDateString, string endDateString)
    {
        var startDate = DateTime.Parse(startDateString, CultureInfo.InvariantCulture).ToUniversalTime();
        var endDate = DateTime.Parse(endDateString, CultureInfo.InvariantCulture).ToUniversalTime();
        tourContext.UpdateResult = tourContext.Tour.UpdateSchedule(startDate, endDate);
    }

    [Then("the tour schedule update should succeed")]
    public void ThenTheTourScheduleUpdateShouldSucceed()
    {
        Assert.NotNull(tourContext.UpdateResult);
        Assert.True(tourContext.UpdateResult.Value.IsSuccess,
            $"Expected success but got error: {tourContext.UpdateResult.Value.ErrorDetails?.Detail}");
    }

    [Then("the tour schedule update should fail")]
    public void ThenTheTourScheduleUpdateShouldFail()
    {
        Assert.NotNull(tourContext.UpdateResult);
        Assert.False(tourContext.UpdateResult.Value.IsSuccess);
    }

    [Then(@"the tour start date should be ""(.*)""")]
    public void ThenTheTourStartDateShouldBe(string expectedDateString)
    {
        var expectedDate = DateTime.Parse(expectedDateString, CultureInfo.InvariantCulture).ToUniversalTime();
        Assert.Equal(expectedDate, tourContext.Tour.Schedule.StartDate);
    }

    [Then(@"the tour end date should be ""(.*)""")]
    public void ThenTheTourEndDateShouldBe(string expectedDateString)
    {
        var expectedDate = DateTime.Parse(expectedDateString, CultureInfo.InvariantCulture).ToUniversalTime();
        Assert.Equal(expectedDate, tourContext.Tour.Schedule.EndDate);
    }
}
