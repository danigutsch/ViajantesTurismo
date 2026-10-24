namespace ViajantesTurismo.Admin.Infrastructure.Branding;

/// <summary>Names the Admin schema that holds only the Branding projection and its inbox.</summary>
internal static class BrandingProjectionSchema
{
    /// <summary>The schema name; a dedicated schema keeps the projection worker's grants narrow.</summary>
    public const string Name = "branding_projection";
}
