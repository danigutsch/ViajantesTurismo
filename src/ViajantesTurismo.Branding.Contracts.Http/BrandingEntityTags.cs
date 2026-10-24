using System.Globalization;

namespace ViajantesTurismo.Branding.Contracts.Http;

/// <summary>Formats the strong entity tag that identifies one Branding settings revision.</summary>
public static class BrandingEntityTags
{
    /// <summary>Formats the quoted strong entity tag <c>"&lt;epoch&gt;:&lt;revision&gt;"</c>.</summary>
    /// <param name="sourceEpoch">The source lineage epoch.</param>
    /// <param name="sourceRevision">The positive source revision.</param>
    /// <returns>The quoted entity tag.</returns>
    public static string Format(Guid sourceEpoch, long sourceRevision)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sourceRevision);

        return string.Create(CultureInfo.InvariantCulture, $"\"{sourceEpoch:N}:{sourceRevision}\"");
    }
}
