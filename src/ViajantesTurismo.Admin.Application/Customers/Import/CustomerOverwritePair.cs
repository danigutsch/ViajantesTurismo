using ViajantesTurismo.Admin.Domain.Customers;

namespace ViajantesTurismo.Admin.Application.Customers.Import;

/// <summary>
/// Represents an overwrite operation between an existing customer and incoming imported data.
/// </summary>
/// <param name="ExistingCustomer">The existing customer to be updated.</param>
/// <param name="IncomingCustomer">The incoming customer data used for overwriting.</param>
public sealed record CustomerOverwritePair(Customer ExistingCustomer, Customer IncomingCustomer);
