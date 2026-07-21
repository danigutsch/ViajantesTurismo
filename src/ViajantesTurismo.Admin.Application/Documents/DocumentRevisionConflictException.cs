namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Signals that persistence rejected a duplicate document revision lineage key.</summary>
public sealed class DocumentRevisionConflictException : Exception
{
    private const string DefaultMessage = "A document revision already exists for this booking.";

    /// <summary>Initializes the exception with the default conflict message.</summary>
    public DocumentRevisionConflictException()
        : base(DefaultMessage)
    {
    }

    /// <summary>Initializes the exception with the supplied conflict message.</summary>
    /// <param name="message">The conflict message.</param>
    public DocumentRevisionConflictException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes the exception with the supplied conflict message and cause.</summary>
    /// <param name="message">The conflict message.</param>
    /// <param name="innerException">The persistence failure that caused the conflict.</param>
    public DocumentRevisionConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Initializes the exception with the default conflict message and cause.</summary>
    /// <param name="innerException">The persistence failure that caused the conflict.</param>
    public DocumentRevisionConflictException(Exception innerException)
        : this(DefaultMessage, innerException)
    {
    }
}
