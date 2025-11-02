using JetBrains.Annotations;
using ViajantesTurismo.Admin.Domain.Customers;

namespace ViajantesTurismo.Admin.BehaviorTests.Context;

[UsedImplicitly]
public sealed class AddressContext
{
    public required Address Address { get; set; }
    public required string Street { get; set; }
    public required string Complement { get; set; }
    public required string Neighborhood { get; set; }
    public required string PostalCode { get; set; }
    public required string City { get; set; }
    public required string State { get; set; }
    public required string Country { get; set; }
}
