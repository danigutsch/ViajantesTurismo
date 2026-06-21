namespace ViajantesTurismo.Admin.SystemTests.Infrastructure.Bases;

[AttributeUsage(AttributeTargets.Method)]
internal sealed class SerialE2EReasonAttribute(string reason) : Attribute
{
    public string Reason { get; } = reason;
}
