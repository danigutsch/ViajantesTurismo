using System.Globalization;
using Reqnroll;
using ViajantesTurismo.Admin.BehaviorTests.Context;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps;

[Binding]
public sealed class TourUpdateScheduleSteps(TourContext tourContext)
{
    [Given(@"a tour exists with dates from ""(.*)"" to ""(.*)""")]
    public void GivenATourExistsWithDatesFromTo(string startDateString, string endDateString)
    {
        var startDate = DateTime.Parse(startDateString, CultureInfo.InvariantCulture).ToUniversalTime();
        var endDate = DateTime.Parse(endDateString, CultureInfo.InvariantCulture).ToUniversalTime();
        tourContext.Tour = TestHelpers.CreateTestTourWithDates(startDate, endDate);
    }

    [When(@"I update the tour schedule to start ""(.*)"" and end ""(.*)""")]
    public void WhenIUpdateTheTourScheduleToStartAndEnd(string startDateString, string endDateString)
    {
        var startDate = DateTime.Parse(startDateString, CultureInfo.InvariantCulture).ToUniversalTime();
        var endDate = DateTime.Parse(endDateString, CultureInfo.InvariantCulture).ToUniversalTime();
        tourContext.Result = tourContext.Tour.UpdateSchedule(startDate, endDate);
    }

    [Then(@"the tour schedule update should succeed")]
    public void ThenTheTourScheduleUpdateShouldSucceed()
    {
        var result = (Result)tourContext.Result;
        Assert.True(result.IsSuccess, $"Expected success but got error: {result.ErrorDetails?.Detail}");
    }

    [Then(@"the tour schedule update should fail")]
    public void ThenTheTourScheduleUpdateShouldFail()
    {
        var result = (Result)tourContext.Result;
        Assert.False(result.IsSuccess);
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