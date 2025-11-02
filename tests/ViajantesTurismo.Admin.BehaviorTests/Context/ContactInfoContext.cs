using JetBrains.Annotations;
using ViajantesTurismo.Admin.Domain.Customers;

namespace ViajantesTurismo.Admin.BehaviorTests.Context;

[UsedImplicitly]
public sealed class ContactInfoContext
{
    public required ContactInfo ContactInfo { get; set; }
    public required string Email { get; set; }
    public required string Mobile { get; set; }
    public string? Instagram { get; set; }
    public string? Facebook { get; set; }
}
