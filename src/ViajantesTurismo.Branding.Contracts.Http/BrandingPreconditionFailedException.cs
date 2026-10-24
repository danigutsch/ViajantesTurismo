namespace ViajantesTurismo.Branding.Contracts.Http;

/// <summary>
/// Thrown when another editor saved Branding after this editor's read. The caller reloads with a new read and
/// deliberately reapplies its changes; it never retries the same write.
/// </summary>
public sealed class BrandingPreconditionFailedException : Exception
{
    /// <summary>Creates the exception.</summary>
    public BrandingPreconditionFailedException()
        : base("Branding settings changed after they were loaded.")
    {
    }

    /// <summary>Creates the exception with a message.</summary>
    /// <param name="message">The message.</param>
    public BrandingPreconditionFailedException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with a message and inner exception.</summary>
    /// <param name="message">The message.</param>
    /// <param name="innerException">The inner exception.</param>
    public BrandingPreconditionFailedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
