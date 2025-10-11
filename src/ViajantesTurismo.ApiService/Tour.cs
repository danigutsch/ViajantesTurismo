using ViajantesTurismo.Common;
using ViajantesTurismo.Common.Monies;

namespace ViajantesTurismo.ApiService;

internal sealed class Tour : Entity<int>
{
    public Tour(string identifier,
        string name,
        DateTime startDate,
        DateTime endDate,
        decimal price,
        decimal singleRoomSupplementPrice,
        decimal regularBikePrice,
        decimal eBikePrice,
        Currency currency,
        IEnumerable<string> includedServices)
    {
        Identifier = identifier;
        Name = name;
        StartDate = startDate;
        EndDate = endDate;
        Price = price;
        SingleRoomSupplementPrice = singleRoomSupplementPrice;
        RegularBikePrice = regularBikePrice;
        EBikePrice = eBikePrice;
        Currency = currency;
        IncludedServices = [..includedServices];
    }

    public string Identifier { get; }
    public string Name { get; }
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }
    public decimal Price { get; }
    public decimal SingleRoomSupplementPrice { get; }
    public decimal RegularBikePrice { get; }
    public decimal EBikePrice { get; }
    public Currency Currency { get; }
    public string[] IncludedServices { get; }

    // Parameterless constructor for EF Core
#pragma warning disable CS8618
    public Tour()
    {
    }
}