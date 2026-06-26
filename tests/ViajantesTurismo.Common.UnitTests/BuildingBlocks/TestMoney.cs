using ViajantesTurismo.Common.BuildingBlocks;

namespace ViajantesTurismo.Common.UnitTests.BuildingBlocks;

internal sealed class TestMoney(decimal amount, string currency) : ValueObject
{
    public decimal Amount { get; } = amount;

    public string Currency { get; } = currency;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
