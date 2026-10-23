namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Contains immutable draft, approved or superseded EUR terms.</summary>
public sealed record GetPaymentScheduleDto
{
    /// <summary>Gets Id.</summary>
    public required Guid Id
    {
        get; init;
    }
    /// <summary>Gets Revision.</summary>
    public required int Revision
    {
        get; init;
    }
    /// <summary>Gets ObligationEuro.</summary>
    public required decimal ObligationEuro
    {
        get; init;
    }
    /// <summary>Gets BookingPriceEuro.</summary>
    public required decimal BookingPriceEuro
    {
        get; init;
    }
    /// <summary>Gets Status.</summary>
    public required PaymentScheduleStatusDto Status
    {
        get; init;
    }
    /// <summary>Gets ApprovalWithdrawn.</summary>
    public required bool ApprovalWithdrawn
    {
        get; init;
    }
    /// <summary>Gets Approval.</summary>
    public required FinancialApprovalDto Approval
    {
        get; init;
    }
    /// <summary>Gets Installments.</summary>
    public required IReadOnlyList<GetPaymentInstallmentDto> Installments
    {
        get; init;
    }
    /// <inheritdoc />
    public override string ToString() => "GetPaymentScheduleDto [redacted]";
}
