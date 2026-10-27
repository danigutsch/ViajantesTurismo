namespace ViajantesTurismo.Management.WebTests;

/// <summary>Trait names and values used to tag Management web tests.</summary>
internal static class TestTraits
{
    /// <summary>Scope value for unit tests.</summary>
    public const string UnitScope = "unit";
    /// <summary>Scope value for component tests.</summary>
    public const string ComponentScope = "component";
    /// <summary>Area value for the Management web application.</summary>
    public const string ManagementWebArea = "management-web";
    /// <summary>Category value for API client tests.</summary>
    public const string ApiClientCategory = "api-client";
    /// <summary>Category value for component tests.</summary>
    public const string ComponentCategory = "component";
    /// <summary>Category value for endpoint tests.</summary>
    public const string EndpointCategory = PandaBear.Testing.TestTraitValues.EndpointCategory;
    /// <summary>Category value for security tests.</summary>
    public const string SecurityCategory = PandaBear.Testing.TestTraitValues.SecurityCategory;
}
