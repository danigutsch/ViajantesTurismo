namespace ViajantesTurismo.Admin.Application.Branding;

/// <summary>
/// Thrown when a Branding event cannot be applied because the projection is fenced for recovery. The transport keeps
/// the event pending, so it is delivered after recovery instead of being acknowledged or lost.
/// </summary>
public sealed class BrandingProjectionRecoveringException : Exception
{
    /// <summary>Creates the exception.</summary>
    public BrandingProjectionRecoveringException()
        : base("The Branding projection is recovering; ordinary delivery is fenced.")
    {
    }

    /// <summary>Creates the exception with a message.</summary>
    /// <param name="message">The message.</param>
    public BrandingProjectionRecoveringException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with a message and inner exception.</summary>
    /// <param name="message">The message.</param>
    /// <param name="innerException">The inner exception.</param>
    public BrandingProjectionRecoveringException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
