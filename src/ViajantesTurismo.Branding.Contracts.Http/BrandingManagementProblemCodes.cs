namespace ViajantesTurismo.Branding.Contracts.Http;

/// <summary>Problem codes returned by the Branding management settings endpoint.</summary>
public static class BrandingManagementProblemCodes
{
    /// <summary>The problem-details extension member that carries the code.</summary>
    public const string ExtensionName = "code";

    /// <summary>The <c>PUT</c> omitted <c>If-Match</c> (428).</summary>
    public const string PreconditionRequired = "branding.precondition_required";

    /// <summary>The <c>If-Match</c> field value is not valid RFC 9110 syntax (400).</summary>
    public const string InvalidIfMatch = "branding.invalid_if_match";

    /// <summary>The <c>If-Match</c> condition did not strongly match the current revision (412).</summary>
    public const string PreconditionFailed = "branding.precondition_failed";
}
