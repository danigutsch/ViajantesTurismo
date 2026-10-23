namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Defines payment account reason labels.</summary>
public enum PaymentAccountReasonDto
{
    /// <summary>No reason selected.</summary>
    None = 0,
    /// <summary>A recipient draft was created.</summary>
    DraftCreated = 1,
    /// <summary>Recipient details were replaced with an unverified revision.</summary>
    RecipientChanged = 2,
    /// <summary>An independent bank or recipient check was attested.</summary>
    BankCheckConfirmed = 3,
    /// <summary>An independent review accepted the revision.</summary>
    ReviewAccepted = 4,
    /// <summary>An independent review requested changes.</summary>
    ReviewChangesRequested = 5,
    /// <summary>An Admin explicitly withdrew verification.</summary>
    ApprovalWithdrawn = 6,
    /// <summary>The account is no longer used.</summary>
    NoLongerUsed = 7,
    /// <summary>The account was replaced.</summary>
    AccountReplaced = 8,
    /// <summary>An Admin explicitly accessed recipient details.</summary>
    SensitiveAccess = 9,
}
