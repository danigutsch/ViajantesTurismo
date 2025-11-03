using System.Globalization;
using Reqnroll;
using ViajantesTurismo.Admin.BehaviorTests.Context;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Common.Monies;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps;

[Binding]
[Scope(Feature = "Tour Included Services Validation")]
public sealed class TourIncludedServicesValidationSteps(TourContext tourContext)
{
    private static readonly string[] DefaultService = ["Default Service"];
    private List<string> _servicesToUpdate = new();

    [Given(@"a valid tour exists with the following details:")]
    public void GivenAValidTourExistsWithTheFollowingDetails(Table table)
    {
        var data = table.Rows.ToDictionary(row => row["Field"], row => row["Value"]);

        var startDate = DateTime.Parse(data["StartDate"], CultureInfo.InvariantCulture).ToUniversalTime();
        var endDate = DateTime.Parse(data["EndDate"], CultureInfo.InvariantCulture).ToUniversalTime();
        var price = decimal.Parse(data["Price"], CultureInfo.InvariantCulture);
        var doubleRoomSupplementPrice = decimal.Parse(data["DoubleRoomSupplementPrice"], CultureInfo.InvariantCulture);
        var regularBikePrice = decimal.Parse(data["RegularBikePrice"], CultureInfo.InvariantCulture);
        var eBikePrice = decimal.Parse(data["EBikePrice"], CultureInfo.InvariantCulture);
        var currency = ParseCurrency(data["Currency"]);

        tourContext.Tour = Tour.Create(
            identifier: data["Identifier"],
            name: data["Name"],
            startDate: startDate,
            endDate: endDate,
            price: price,
            doubleRoomSupplementPrice: doubleRoomSupplementPrice,
            regularBikePrice: regularBikePrice,
            eBikePrice: eBikePrice,
            currency: currency,
            includedServices: DefaultService).Value;
    }

    [When(@"I update the tour's included services with:")]
    public void WhenIUpdateTheToursIncludedServicesWith(Table table)
    {
        _servicesToUpdate = table.Rows.Select(row => row["Service"]).ToList();
        tourContext.Tour.UpdateIncludedServices(_servicesToUpdate);
        tourContext.UpdateResult = Result.Ok();
    }

    [When(@"I update the tour's included services with an empty list")]
    public void WhenIUpdateTheToursIncludedServicesWithAnEmptyList()
    {
        _servicesToUpdate = [];
        tourContext.Tour.UpdateIncludedServices(_servicesToUpdate);
        tourContext.UpdateResult = Result.Ok();
    }

    [When(@"I update the tour's included services with services containing extra whitespace")]
    public void WhenIUpdateTheToursIncludedServicesWithServicesContainingExtraWhitespace()
    {
        _servicesToUpdate =
        [
            "  Hotel  ",
            "Breakfast   with    extra   spaces",
            "   City  Tour   "
        ];
        tourContext.Tour.UpdateIncludedServices(_servicesToUpdate);
        tourContext.UpdateResult = Result.Ok();
    }

    [Then(@"the tour update should succeed")]
    public void ThenTheTourUpdateShouldSucceed()
    {
        Assert.NotNull(tourContext.UpdateResult);
        var result = tourContext.UpdateResult.Value;
        Assert.True(result.IsSuccess,
            $"Expected success but got failure: {result.ErrorDetails?.Detail ?? "Unknown error"}");
    }

    [Then(@"the tour should have (\d+) included services")]
    public void ThenTheTourShouldHaveDIncludedServices(int expectedCount)
    {
        Assert.Equal(expectedCount, tourContext.Tour.IncludedServices.Count);
    }

    [Then(@"the services should be properly sanitized")]
    public void ThenTheServicesShouldBeProperlySanitized()
    {
        var services = tourContext.Tour.IncludedServices;

        foreach (var service in services)
        {
            Assert.Equal(service.Trim(), service);
        }

        Assert.DoesNotContain(services, s => s.Contains("  ", StringComparison.Ordinal));
    }

    [Then(@"the included services should contain ""(.*)""")]
    public void ThenTheIncludedServicesShouldContain(string expectedService)
    {
        Assert.Contains(expectedService, tourContext.Tour.IncludedServices, StringComparer.Ordinal);
    }

    private static Currency ParseCurrency(string currencyCode) => currencyCode.ToUpperInvariant() switch
    {
        "USD" => Currency.UsDollar,
        "EUR" => Currency.Euro,
        "BRL" => Currency.Real,
        _ => throw new ArgumentException($"Unknown currency code: {currencyCode}")
    };
}