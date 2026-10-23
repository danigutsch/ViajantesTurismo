using PandaBear.BuildingBlocks;
using PandaBear.Results;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Domain.PaymentAccounts;

/// <summary>Contains validated recipient details; format validation never establishes ownership.</summary>
public sealed class PaymentRecipientDetails : ValueObject
{
    private readonly PaymentRecipientState _state;

    private PaymentRecipientDetails(PaymentRecipientState state) => _state = state;

    /// <summary>Gets the selected recipient rail.</summary>
    public PaymentAccountKind Kind => _state.Kind;

    /// <summary>Gets the independently checked account settlement currency.</summary>
    public Currency SettlementCurrency => _state.SettlementCurrency;

    /// <summary>Gets the sensitive recipient name.</summary>
    public string RecipientName => _state.RecipientName;

    /// <summary>Gets sensitive canonical IBAN details for explicitly authorized instruction rendering. Do not log.</summary>
    public Iban? BankIban => _state.Iban is { } value ? Iban.Create(value).Value : null;
    /// <summary>Gets sensitive canonical BIC details for explicitly authorized instruction rendering. Do not log.</summary>
    public Bic? BankBic => _state.Bic is { } value ? Bic.Create(value).Value : null;
    /// <summary>Gets the sensitive typed Pix key for explicitly authorized instruction rendering. Do not log.</summary>
    public PixKey? Pix => _state.PixKey is { } value ? PixKey.Create(_state.PixType, value).Value : null;
    /// <summary>Gets the bank participant identifier. Do not log recipient details.</summary>
    public string? BankIspb => _state.Ispb;
    /// <summary>Gets the branch without its verifier. Do not log recipient details.</summary>
    public string? BankBranch => _state.Branch;
    /// <summary>Gets the sensitive account number including its verifier. Do not log.</summary>
    public string? BankAccountNumber => _state.AccountNumber;
    /// <summary>Gets the verified Brazilian account type.</summary>
    public BrazilianAccountType BankAccountType => _state.AccountType;
    /// <summary>Gets the sensitive account holder taxpayer identifier. Do not log.</summary>
    public string? HolderTaxId => _state.TaxId;

    /// <summary>Gets a display-only masked identifier without the recipient name.</summary>
    public string MaskedIdentifier
    {
        get
        {
            var value = _state.Iban ?? _state.PixKey ?? _state.AccountNumber ?? string.Empty;
            return value.Length > 4 ? "…" + value[^4..] : new string('*', value.Length);
        }
    }

    /// <summary>Creates an IBAN recipient from canonical identifiers.</summary>
    /// <param name="recipientName">The recipient's bank-recorded name.</param>
    /// <param name="iban">The canonical IBAN.</param>
    /// <param name="bic">The canonical ISO 9362 BIC.</param>
    /// <param name="settlementCurrency">The bank-checked EUR or BRL account currency.</param>
    /// <returns>Validated details or an input-independent failure.</returns>
    public static Result<PaymentRecipientDetails> CreateIban(string recipientName, string? iban, string? bic, Currency settlementCurrency = Currency.Euro) =>
        Restore(new PaymentRecipientState(PaymentAccountKind.Iban, recipientName, iban, bic, PixKeyType.None, null, null, null, null, BrazilianAccountType.None, null, settlementCurrency));

    /// <summary>Creates a recipient addressed through an explicitly selected Pix key type.</summary>
    /// <param name="recipientName">The recipient's bank-recorded name.</param>
    /// <param name="keyType">The explicitly selected Pix format.</param>
    /// <param name="key">The canonical key.</param>
    /// <returns>Validated details or an input-independent failure.</returns>
    public static Result<PaymentRecipientDetails> CreatePix(string recipientName, PixKeyType keyType, string? key) =>
        Restore(new PaymentRecipientState(PaymentAccountKind.Pix, recipientName, null, null, keyType, key, null, null, null, BrazilianAccountType.None, null, Currency.Real));

    /// <summary>Creates Brazilian bank details using BCB DICT field formats and taxpayer check digits.</summary>
    /// <param name="recipientName">The recipient's bank-recorded name.</param>
    /// <param name="ispb">The canonical eight-character bank participant identifier.</param>
    /// <param name="branch">The optional one-to-four digit branch without its check digit.</param>
    /// <param name="accountNumber">The account including its verifier, in BCB canonical format.</param>
    /// <param name="accountType">The selected bank account type.</param>
    /// <param name="taxId">The account holder's canonical CPF or CNPJ.</param>
    /// <returns>Validated details; bank-specific account ownership/checks still require attestation.</returns>
    public static Result<PaymentRecipientDetails> CreateBrazilian(
        string recipientName, string? ispb, string? branch, string? accountNumber, BrazilianAccountType accountType, string? taxId) =>
        Restore(new PaymentRecipientState(PaymentAccountKind.BrazilianBank, recipientName, null, null, PixKeyType.None, null, ispb, branch, accountNumber, accountType, taxId, Currency.Real));

    internal PaymentRecipientState ExportState() => _state;

    internal static Result<PaymentRecipientDetails> Restore(PaymentRecipientState? state)
    {
        if (state is null || state.SettlementCurrency is not (Currency.Euro or Currency.Real) || string.IsNullOrWhiteSpace(state.RecipientName)
            || state.RecipientName.Length > AdminDomainLimits.MaxNameLength
            || state.RecipientName.Any(char.IsControl) || state.RecipientName != state.RecipientName.Trim())
        {
            return PaymentAccountErrors.Invalid("recipientName").ConvertError<PaymentRecipientDetails>();
        }

        var valid = state.Kind switch
        {
            PaymentAccountKind.Iban => Iban.Create(state.Iban).IsSuccess && Bic.Create(state.Bic).IsSuccess
                && state.PixKey is null && state.PixType == PixKeyType.None && BankFieldsEmpty(state),
            PaymentAccountKind.Pix => state.SettlementCurrency == Currency.Real && PixKey.Create(state.PixType, state.PixKey).IsSuccess
                && state.Iban is null && state.Bic is null && BankFieldsEmpty(state),
            PaymentAccountKind.BrazilianBank => state.SettlementCurrency == Currency.Real && CanonicalAlphanumeric(state.Ispb, 8, 8)
                && (state.Branch is null || state.Branch.Length is >= 1 and <= 4
                    && state.Branch.All(char.IsAsciiDigit))
                && CanonicalAlphanumeric(state.AccountNumber, 1, 20)
                && state.AccountType is BrazilianAccountType.Checking or BrazilianAccountType.Salary or BrazilianAccountType.Savings or BrazilianAccountType.Payment
                && (Cpf.Create(state.TaxId).IsSuccess || Cnpj.Create(state.TaxId).IsSuccess)
                && state.Iban is null && state.Bic is null && state.PixKey is null && state.PixType == PixKeyType.None,
            _ => false
        };

        return valid ? Result.Ok(new PaymentRecipientDetails(state))
            : PaymentAccountErrors.Invalid("recipient").ConvertError<PaymentRecipientDetails>();
    }

    /// <inheritdoc />
    public override string ToString() => "PaymentRecipientDetails [redacted]";

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return _state;
    }

    private static bool BankFieldsEmpty(PaymentRecipientState state) =>
        state.Ispb is null && state.Branch is null && state.AccountNumber is null
        && state.AccountType == BrazilianAccountType.None && state.TaxId is null;

    private static bool CanonicalAlphanumeric(string? value, int minimum, int maximum) =>
        value is not null && value.Length >= minimum && value.Length <= maximum
        && value.All(static character => char.IsAsciiDigit(character) || character is >= 'A' and <= 'Z');
}
