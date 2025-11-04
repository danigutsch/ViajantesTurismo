using JetBrains.Annotations;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.BehaviorTests.Context;

[UsedImplicitly]
public sealed class ContactInfoContext
{
    public ContactInfo ContactInfo { get; set; } = null!;
    public required string Email { get; set; }
    public required string Mobile { get; set; }
    public string? Instagram { get; set; }
    public string? Facebook { get; set; }
    public required Result<ContactInfo> Result { get; set; }
}