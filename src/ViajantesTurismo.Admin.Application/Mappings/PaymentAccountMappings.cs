using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.PaymentAccounts;
using PandaBear.BuildingBlocks;
using PandaBear.Results;

namespace ViajantesTurismo.Admin.Application.Mappings;

/// <summary>Maps validated payment recipients and masked, contract-owned staff projections.</summary>
public static class PaymentAccountMappings
{
    /// <summary>Maps the selected PaymentAccountKind without accepting undefined labels.</summary>
    public static PaymentAccountKindDto Map(PaymentAccountKind value) => value switch
    {
        PaymentAccountKind.None => PaymentAccountKindDto.None,
        PaymentAccountKind.Iban => PaymentAccountKindDto.Iban,
        PaymentAccountKind.BrazilianBank => PaymentAccountKindDto.BrazilianBank,
        PaymentAccountKind.Pix => PaymentAccountKindDto.Pix,
        _ => throw new ArgumentOutOfRangeException(nameof(value), "Unknown payment account label.")
    };

    /// <summary>Maps the selected PaymentAccountKind without accepting undefined labels.</summary>
    public static PaymentAccountKind Map(PaymentAccountKindDto value) => value switch
    {
        PaymentAccountKindDto.None => PaymentAccountKind.None,
        PaymentAccountKindDto.Iban => PaymentAccountKind.Iban,
        PaymentAccountKindDto.BrazilianBank => PaymentAccountKind.BrazilianBank,
        PaymentAccountKindDto.Pix => PaymentAccountKind.Pix,
        _ => throw new ArgumentOutOfRangeException(nameof(value), "Unknown payment account label.")
    };

    /// <summary>Maps the selected PaymentAccountStatus without accepting undefined labels.</summary>
    public static PaymentAccountStatusDto Map(PaymentAccountStatus value) => value switch
    {
        PaymentAccountStatus.Draft => PaymentAccountStatusDto.Draft,
        PaymentAccountStatus.Verified => PaymentAccountStatusDto.Verified,
        PaymentAccountStatus.Inactive => PaymentAccountStatusDto.Inactive,
        _ => throw new ArgumentOutOfRangeException(nameof(value), "Unknown payment account label.")
    };

    /// <summary>Maps the selected PaymentAccountStatus without accepting undefined labels.</summary>
    public static PaymentAccountStatus Map(PaymentAccountStatusDto value) => value switch
    {
        PaymentAccountStatusDto.Draft => PaymentAccountStatus.Draft,
        PaymentAccountStatusDto.Verified => PaymentAccountStatus.Verified,
        PaymentAccountStatusDto.Inactive => PaymentAccountStatus.Inactive,
        _ => throw new ArgumentOutOfRangeException(nameof(value), "Unknown payment account label.")
    };

    /// <summary>Maps the selected BrazilianAccountType without accepting undefined labels.</summary>
    public static BrazilianAccountTypeDto Map(BrazilianAccountType value) => value switch
    {
        BrazilianAccountType.None => BrazilianAccountTypeDto.None,
        BrazilianAccountType.Checking => BrazilianAccountTypeDto.Checking,
        BrazilianAccountType.Salary => BrazilianAccountTypeDto.Salary,
        BrazilianAccountType.Savings => BrazilianAccountTypeDto.Savings,
        BrazilianAccountType.Payment => BrazilianAccountTypeDto.Payment,
        _ => throw new ArgumentOutOfRangeException(nameof(value), "Unknown payment account label.")
    };

    /// <summary>Maps the selected BrazilianAccountType without accepting undefined labels.</summary>
    public static BrazilianAccountType Map(BrazilianAccountTypeDto value) => value switch
    {
        BrazilianAccountTypeDto.None => BrazilianAccountType.None,
        BrazilianAccountTypeDto.Checking => BrazilianAccountType.Checking,
        BrazilianAccountTypeDto.Salary => BrazilianAccountType.Salary,
        BrazilianAccountTypeDto.Savings => BrazilianAccountType.Savings,
        BrazilianAccountTypeDto.Payment => BrazilianAccountType.Payment,
        _ => throw new ArgumentOutOfRangeException(nameof(value), "Unknown payment account label.")
    };

    /// <summary>Maps the selected PaymentAccountVerificationMethod without accepting undefined labels.</summary>
    public static PaymentAccountVerificationMethodDto Map(PaymentAccountVerificationMethod value) => value switch
    {
        PaymentAccountVerificationMethod.None => PaymentAccountVerificationMethodDto.None,
        PaymentAccountVerificationMethod.BankStatement => PaymentAccountVerificationMethodDto.BankStatement,
        PaymentAccountVerificationMethod.BankConfirmation => PaymentAccountVerificationMethodDto.BankConfirmation,
        PaymentAccountVerificationMethod.RecipientConfirmation => PaymentAccountVerificationMethodDto.RecipientConfirmation,
        _ => throw new ArgumentOutOfRangeException(nameof(value), "Unknown payment account label.")
    };

    /// <summary>Maps the selected PaymentAccountVerificationMethod without accepting undefined labels.</summary>
    public static PaymentAccountVerificationMethod Map(PaymentAccountVerificationMethodDto value) => value switch
    {
        PaymentAccountVerificationMethodDto.None => PaymentAccountVerificationMethod.None,
        PaymentAccountVerificationMethodDto.BankStatement => PaymentAccountVerificationMethod.BankStatement,
        PaymentAccountVerificationMethodDto.BankConfirmation => PaymentAccountVerificationMethod.BankConfirmation,
        PaymentAccountVerificationMethodDto.RecipientConfirmation => PaymentAccountVerificationMethod.RecipientConfirmation,
        _ => throw new ArgumentOutOfRangeException(nameof(value), "Unknown payment account label.")
    };

    /// <summary>Maps the selected PaymentAccountReviewOutcome without accepting undefined labels.</summary>
    public static PaymentAccountReviewOutcomeDto Map(PaymentAccountReviewOutcome value) => value switch
    {
        PaymentAccountReviewOutcome.None => PaymentAccountReviewOutcomeDto.None,
        PaymentAccountReviewOutcome.Approved => PaymentAccountReviewOutcomeDto.Approved,
        PaymentAccountReviewOutcome.ChangesRequested => PaymentAccountReviewOutcomeDto.ChangesRequested,
        _ => throw new ArgumentOutOfRangeException(nameof(value), "Unknown payment account label.")
    };

    /// <summary>Maps the selected PaymentAccountReviewOutcome without accepting undefined labels.</summary>
    public static PaymentAccountReviewOutcome Map(PaymentAccountReviewOutcomeDto value) => value switch
    {
        PaymentAccountReviewOutcomeDto.None => PaymentAccountReviewOutcome.None,
        PaymentAccountReviewOutcomeDto.Approved => PaymentAccountReviewOutcome.Approved,
        PaymentAccountReviewOutcomeDto.ChangesRequested => PaymentAccountReviewOutcome.ChangesRequested,
        _ => throw new ArgumentOutOfRangeException(nameof(value), "Unknown payment account label.")
    };

    /// <summary>Maps the selected PaymentAccountReason without accepting undefined labels.</summary>
    public static PaymentAccountReasonDto Map(PaymentAccountReason value) => value switch
    {
        PaymentAccountReason.None => PaymentAccountReasonDto.None,
        PaymentAccountReason.DraftCreated => PaymentAccountReasonDto.DraftCreated,
        PaymentAccountReason.RecipientChanged => PaymentAccountReasonDto.RecipientChanged,
        PaymentAccountReason.BankCheckConfirmed => PaymentAccountReasonDto.BankCheckConfirmed,
        PaymentAccountReason.ReviewAccepted => PaymentAccountReasonDto.ReviewAccepted,
        PaymentAccountReason.ReviewChangesRequested => PaymentAccountReasonDto.ReviewChangesRequested,
        PaymentAccountReason.ApprovalWithdrawn => PaymentAccountReasonDto.ApprovalWithdrawn,
        PaymentAccountReason.NoLongerUsed => PaymentAccountReasonDto.NoLongerUsed,
        PaymentAccountReason.AccountReplaced => PaymentAccountReasonDto.AccountReplaced,
        PaymentAccountReason.SensitiveAccess => PaymentAccountReasonDto.SensitiveAccess,
        _ => throw new ArgumentOutOfRangeException(nameof(value), "Unknown payment account label.")
    };

    /// <summary>Maps the selected PaymentAccountReason without accepting undefined labels.</summary>
    public static PaymentAccountReason Map(PaymentAccountReasonDto value) => value switch
    {
        PaymentAccountReasonDto.None => PaymentAccountReason.None,
        PaymentAccountReasonDto.DraftCreated => PaymentAccountReason.DraftCreated,
        PaymentAccountReasonDto.RecipientChanged => PaymentAccountReason.RecipientChanged,
        PaymentAccountReasonDto.BankCheckConfirmed => PaymentAccountReason.BankCheckConfirmed,
        PaymentAccountReasonDto.ReviewAccepted => PaymentAccountReason.ReviewAccepted,
        PaymentAccountReasonDto.ReviewChangesRequested => PaymentAccountReason.ReviewChangesRequested,
        PaymentAccountReasonDto.ApprovalWithdrawn => PaymentAccountReason.ApprovalWithdrawn,
        PaymentAccountReasonDto.NoLongerUsed => PaymentAccountReason.NoLongerUsed,
        PaymentAccountReasonDto.AccountReplaced => PaymentAccountReason.AccountReplaced,
        PaymentAccountReasonDto.SensitiveAccess => PaymentAccountReason.SensitiveAccess,
        _ => throw new ArgumentOutOfRangeException(nameof(value), "Unknown payment account label.")
    };

    /// <summary>Validates canonical recipient details and rejects unrelated rail fields.</summary>
    public static Result<PaymentRecipientDetails> ToRecipient(PaymentRecipientDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (dto.SettlementCurrency is not (CurrencyDto.Euro or CurrencyDto.Real) || !Enum.IsDefined(dto.Kind) || !Enum.IsDefined(dto.PixKeyType) || !Enum.IsDefined(dto.AccountType))
        {
            return PaymentAccountErrors.Invalid("recipient").ConvertError<PaymentRecipientDetails>();
        }
        var emptyBank = dto.Ispb is null && dto.Branch is null && dto.AccountNumber is null && dto.TaxId is null && dto.AccountType == BrazilianAccountTypeDto.None;
        return dto.Kind switch
        {
            PaymentAccountKindDto.Iban when emptyBank && dto.PixKey is null && dto.PixKeyType == PixKeyTypeDto.None => PaymentRecipientDetails.CreateIban(dto.RecipientName, dto.Iban, dto.Bic, TourMapper.MapToCurrency(dto.SettlementCurrency)),
            PaymentAccountKindDto.Pix when dto.SettlementCurrency == CurrencyDto.Real && emptyBank && dto.Iban is null && dto.Bic is null => PaymentRecipientDetails.CreatePix(dto.RecipientName, Map(dto.PixKeyType), dto.PixKey),
            PaymentAccountKindDto.BrazilianBank when dto.SettlementCurrency == CurrencyDto.Real && dto.Iban is null && dto.Bic is null && dto.PixKey is null && dto.PixKeyType == PixKeyTypeDto.None => PaymentRecipientDetails.CreateBrazilian(dto.RecipientName, dto.Ispb, dto.Branch, dto.AccountNumber, Map(dto.AccountType), dto.TaxId),
            _ => PaymentAccountErrors.Invalid("recipient").ConvertError<PaymentRecipientDetails>()
        };
    }

    /// <summary>Creates an ordinary masked projection without recipient names or identifiers.</summary>
    public static GetPaymentAccountDto ToSummary(PaymentAccount account)
    {
        ArgumentNullException.ThrowIfNull(account);
        return new GetPaymentAccountDto
        {
            SettlementCurrency = TourMapper.MapToCurrencyDto(account.Current.Recipient.SettlementCurrency),
            Id = account.Id,
            Version = account.Version,
            Label = account.Label,
            Kind = Map(account.Current.Recipient.Kind),
            Status = Map(account.Status),
            RevisionId = account.Current.Id,
            Revision = account.Current.Number,
            MaskedIdentifier = account.Current.Recipient.MaskedIdentifier,
            CreatedBy = account.Current.CreatedBy,
            CreatedAt = account.Current.CreatedAt,
            VerifiedBy = account.Current.VerifiedBy,
            VerifiedAt = account.Current.VerifiedAt,
            VerificationMethod = Map(account.Current.VerificationMethod),
            CheckedAt = account.Current.CheckedAt,
            SelfApproved = account.Current.IsSelfApproved,
            ReviewedBy = account.ReviewedBy,
            ReviewedAt = account.ReviewedAt,
            ReviewOutcome = Map(account.ReviewOutcome),
            RequiresAttention = account.RequiresAttention
        };
    }

    private static PixKeyType Map(PixKeyTypeDto value) => value switch
    {
        PixKeyTypeDto.None => PixKeyType.None,
        PixKeyTypeDto.Cpf => PixKeyType.Cpf,
        PixKeyTypeDto.Cnpj => PixKeyType.Cnpj,
        PixKeyTypeDto.Phone => PixKeyType.Phone,
        PixKeyTypeDto.Email => PixKeyType.Email,
        PixKeyTypeDto.Evp => PixKeyType.Evp,
        _ => throw new ArgumentOutOfRangeException(nameof(value), "Unknown Pix key type.")
    };
}
