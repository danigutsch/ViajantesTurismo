namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Signals that persistence rejected a document draft for an ineligible booking.</summary>
public sealed class DocumentBookingEligibilityConflictException : Exception
{
    private const string DefaultMessage = "A document draft requires an accepted booking.";

    /// <summary>Initializes the exception with the default conflict message.</summary>
    public DocumentBookingEligibilityConflictException()
        : base(DefaultMessage)
    {
    }

    /// <summary>Initializes the exception with the supplied conflict message.</summary>
    /// <param name="message">The conflict message.</param>
    public DocumentBookingEligibilityConflictException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes the exception with the supplied conflict message and cause.</summary>
    /// <param name="message">The conflict message.</param>
    /// <param name="innerException">The persistence failure that caused the conflict.</param>
    public DocumentBookingEligibilityConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Initializes the exception with the default conflict message and cause.</summary>
    /// <param name="innerException">The persistence failure that caused the conflict.</param>
    public DocumentBookingEligibilityConflictException(Exception innerException)
        : this(DefaultMessage, innerException)
    {
    }
}
