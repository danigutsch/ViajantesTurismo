namespace ViajantesTurismo.Admin.Domain.PaymentAccounts;

/// <summary>Defines Brazilian account type labels.</summary>
public enum BrazilianAccountType
{
    /// <summary>No account type selected.</summary>
    None = 0,
    /// <summary>BCB CACC checking account.</summary>
    Checking = 1,
    /// <summary>BCB SLRY salary account.</summary>
    Salary = 2,
    /// <summary>BCB SVGS savings account.</summary>
    Savings = 3,
    /// <summary>BCB TRAN payment account.</summary>
    Payment = 4,
}
