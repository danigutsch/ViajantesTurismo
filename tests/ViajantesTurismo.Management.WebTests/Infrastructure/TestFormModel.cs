namespace ViajantesTurismo.Management.WebTests.Infrastructure;

/// <summary>Form model used by form validation tests.</summary>
internal sealed class TestFormModel
{
    /// <summary>Email address field.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>First name field.</summary>
    public string FirstName { get; set; } = string.Empty;
}
