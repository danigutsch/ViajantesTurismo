namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Defines payment account verification method labels.</summary>
public enum PaymentAccountVerificationMethodDto
{
    /// <summary>No independent check selected.</summary>
    None = 0,
    /// <summary>An independent bank statement was checked.</summary>
    BankStatement = 1,
    /// <summary>The bank independently confirmed the recipient.</summary>
    BankConfirmation = 2,
    /// <summary>The recipient independently confirmed the details.</summary>
    RecipientConfirmation = 3,
}
