using System.Text;

namespace ViajantesTurismo.Admin.ApiServiceTests.Infrastructure;

/// <summary>Builds raw create-tour request bodies so tests can send currency values the typed contract cannot express.</summary>
internal static class TourRequestJson
{
    /// <summary>Creates a JSON request body whose currency is the given raw JSON token.</summary>
    public static StringContent WithCurrency(string currencyJson) => new(
        $$"""
        {"identifier":"currency-tour","name":"Currency tour","startDate":"2030-01-01","endDate":"2030-01-08","price":1000,"singleRoomSupplementPrice":10,"regularBikePrice":10,"eBikePrice":10,"currency":{{currencyJson}},"includedServices":["Hotel"],"minCustomers":1,"maxCustomers":4}
        """,
        Encoding.UTF8,
        "application/json");
}
