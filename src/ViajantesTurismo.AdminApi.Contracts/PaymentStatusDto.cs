namespace ViajantesTurismo.AdminApi.Contracts;

/// <summary>
/// DTO for payment status.
/// </summary>
public enum PaymentStatusDto
{
    /// <summary>The booking is unpaid.</summary>
    Unpaid = 0,

    /// <summary>The booking is partially paid.</summary>
    PartiallyPaid = 1,

    /// <summary>The booking is fully paid.</summary>
    Paid = 2,

    /// <summary>The booking has been refunded.</summary>
    Refunded = 3
}