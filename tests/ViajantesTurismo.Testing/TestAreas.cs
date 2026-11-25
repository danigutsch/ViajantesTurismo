namespace ViajantesTurismo.Testing;

/// <summary>Application area values used to select tests across bounded contexts.</summary>
public static class TestAreas
{
    /// <summary>Tests of the Branding module and its consumers.</summary>
    public const string Branding = "branding";

    /// <summary>Tests of the Catalog module and its contracts.</summary>
    public const string Catalog = "catalog";

    /// <summary>Tests of the Management Web application and its hosted security boundary.</summary>
    public const string ManagementWeb = "management-web";

    /// <summary>Cross-cutting application coverage without a single business area.</summary>
    public const string Shared = "shared";
}
