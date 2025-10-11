using ViajantesTurismo.Common;
using ViajantesTurismo.Common.Monies;

namespace ViajantesTurismo.Admin.Domain;

/// <summary>
/// Represents a tour entity with details such as identifier, name, dates, pricing, currency, and included services.
/// </summary>
public sealed class Tour : Entity<int>
{
    private readonly List<string> _includedServices = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="Tour"/> class.
    /// </summary>
    /// <param name="identifier">The unique business identifier for the tour (e.g., "CUBA2024").</param>
    /// <param name="name">The descriptive name of the tour.</param>
    /// <param name="startDate">The start date of the tour.</param>
    /// <param name="endDate">The end date of the tour.</param>
    /// <param name="price">The base price of the tour per person.</param>
    /// <param name="singleRoomSupplementPrice">The additional price for a single room supplement.</param>
    /// <param name="regularBikePrice">The price for renting a regular bike.</param>
    /// <param name="eBikePrice">The price for renting an e-bike.</param>
    /// <param name="currency">The currency for all pricing.</param>
    /// <param name="includedServices">The collection of services included in the tour package.</param>
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
        _includedServices = [..includedServices];
    }

    /// <summary>
    /// Gets the unique business identifier for the tour.
    /// </summary>
    public string Identifier { get; }

    /// <summary>
    /// Gets the name of the tour.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the start date of the tour.
    /// </summary>
    public DateTime StartDate { get; }

    /// <summary>
    /// Gets the end date of the tour.
    /// </summary>
    public DateTime EndDate { get; }

    /// <summary>
    /// Gets the base price of the tour per person.
    /// </summary>
    public decimal Price { get; }

    /// <summary>
    /// Gets the additional price for a single room supplement.
    /// </summary>
    public decimal SingleRoomSupplementPrice { get; }

    /// <summary>
    /// Gets the price for renting a regular bike.
    /// </summary>
    public decimal RegularBikePrice { get; }

    /// <summary>
    /// Gets the price for renting an e-bike.
    /// </summary>
    public decimal EBikePrice { get; }

    /// <summary>
    /// Gets the currency used for all pricing in this tour.
    /// </summary>
    public Currency Currency { get; }

    /// <summary>
    /// Gets the array of services included in the tour package (e.g., "Hotel", "Breakfast", "City Tour").
    /// </summary>
    public IReadOnlyList<string> IncludedServices => _includedServices.AsReadOnly();

    /// <summary>
    /// Initializes a new instance of the <see cref="Tour"/> class.
    /// This constructor is required by Entity Framework Core for materialization.
    /// </summary>
#pragma warning disable CS8618
    public Tour()
    {
    }
}
