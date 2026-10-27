using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Testing.Integration.Helpers;

/// <summary>
/// Optional inputs used when creating a tour for integration and E2E tests.
/// </summary>
public sealed class CreateTourOptions
{
    /// <summary>
    /// Gets the minimum customer count.
    /// </summary>
    public int MinCustomers { get; init; } = 1;

    /// <summary>
    /// Gets the maximum customer count.
    /// </summary>
    public int MaxCustomers { get; init; } = 20;

    /// <summary>
    /// Gets the tour currency.
    /// </summary>
    public CurrencyDto Currency { get; init; } = CurrencyDto.Euro;

    /// <summary>
    /// Gets the optional tour identifier override.
    /// </summary>
    public string? Identifier
    {
        get; init;
    }

    /// <summary>
    /// Gets the optional tour name override.
    /// </summary>
    public string? Name
    {
        get; init;
    }

    /// <summary>
    /// Gets the optional tour start date override.
    /// </summary>
    public DateTime? StartDate
    {
        get; init;
    }

    /// <summary>
    /// Gets the optional tour end date override.
    /// </summary>
    public DateTime? EndDate
    {
        get; init;
    }

    /// <summary>
    /// Gets the tour base price.
    /// </summary>
    public decimal Price { get; init; } = 1000m;
}
