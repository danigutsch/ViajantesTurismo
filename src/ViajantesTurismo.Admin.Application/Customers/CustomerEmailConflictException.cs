namespace ViajantesTurismo.Admin.Application.Customers;

/// <summary>Signals that persistence rejected a duplicate Customer email.</summary>
public sealed class CustomerEmailConflictException : Exception
{
    private const string DefaultMessage = "A Customer already exists with that email.";

    /// <summary>Initializes the exception with the default conflict message.</summary>
    public CustomerEmailConflictException()
        : base(DefaultMessage)
    {
    }

    /// <summary>Initializes the exception with the supplied conflict message.</summary>
    /// <param name="message">The conflict message.</param>
    public CustomerEmailConflictException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes the exception with the supplied conflict message and cause.</summary>
    /// <param name="message">The conflict message.</param>
    /// <param name="innerException">The persistence failure that caused the conflict.</param>
    public CustomerEmailConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Initializes the exception with the default conflict message and cause.</summary>
    /// <param name="innerException">The persistence failure that caused the conflict.</param>
    public CustomerEmailConflictException(Exception innerException)
        : this(DefaultMessage, innerException)
    {
    }
}
