using System.Text;

namespace ViajantesTurismo.Branding.Contracts.IntegrationEvents.Branding;

/// <summary>Defines the storage and transport budgets of a Branding settings event.</summary>
public static class BrandingEventLimits
{
    /// <summary>Maximum binary size of a normalized WebP logo.</summary>
    public const int MaxLogoBytes = 40 * 1024;

    /// <summary>Maximum size in UTF-8 bytes of a serialized Branding event payload.</summary>
    public const int MaxPayloadBytes = 64 * 1024;

    /// <summary>Rejects an oversized serialized payload before it is stored or deserialized.</summary>
    /// <param name="payload">The serialized Branding event payload.</param>
    /// <exception cref="InvalidOperationException">The payload exceeds the Branding event budget.</exception>
    public static void EnsurePayloadFits(string payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        if (payload.Length > MaxPayloadBytes || Encoding.UTF8.GetByteCount(payload) > MaxPayloadBytes)
        {
            throw new InvalidOperationException("The Branding event payload exceeds the configured byte limit.");
        }
    }
}
