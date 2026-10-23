namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Defines payment account kind labels.</summary>
public enum PaymentAccountKindDto
{
    /// <summary>No recipient format selected.</summary>
    None = 0,
    /// <summary>An IBAN and BIC recipient.</summary>
    Iban = 1,
    /// <summary>A Brazilian bank account in BCB DICT format.</summary>
    BrazilianBank = 2,
    /// <summary>An explicitly typed Pix recipient.</summary>
    Pix = 3,
}
