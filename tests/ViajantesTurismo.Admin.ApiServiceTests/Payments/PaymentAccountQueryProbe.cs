using ViajantesTurismo.Admin.Application.Payments;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.ApiServiceTests.Payments;

/// <summary>Query service exposing one verified EUR account and counting sensitive-detail reads.</summary>
internal sealed class PaymentAccountQueryProbe : IPaymentAccountQueryService
{
    /// <summary>Gets how many times sensitive account details were read.</summary>
    public int SensitiveReads
    {
        get; private set;
    }
    /// <summary>Gets the single verified account served by this probe.</summary>
    public GetPaymentAccountDto Account
    {
        get;
    } = new()
    {
        Id = Guid.CreateVersion7(),
        RevisionId = Guid.CreateVersion7(),
        Version = 2,
        Revision = 1,
        Label = "EUR account",
        Kind = PaymentAccountKindDto.Iban,
        Status = PaymentAccountStatusDto.Verified,
        SettlementCurrency = CurrencyDto.Euro,
        MaskedIdentifier = "DE…3000",
        CreatedBy = "owner",
        CreatedAt = new DateTimeOffset(2030, 1, 5, 12, 0, 0, TimeSpan.Zero),
        VerifiedBy = "owner",
        VerifiedAt = new DateTimeOffset(2030, 1, 5, 12, 0, 0, TimeSpan.Zero),
        VerificationMethod = PaymentAccountVerificationMethodDto.BankStatement,
        CheckedAt = new DateTimeOffset(2030, 1, 5, 12, 0, 0, TimeSpan.Zero),
        SelfApproved = true,
        ReviewOutcome = PaymentAccountReviewOutcomeDto.None,
        RequiresAttention = false
    };
    /// <summary>Returns the single account.</summary>
    public Task<IReadOnlyList<GetPaymentAccountDto>> List(CancellationToken ct) => Task.FromResult<IReadOnlyList<GetPaymentAccountDto>>([Account]);
    /// <summary>Returns the account when the id matches, otherwise null.</summary>
    public Task<GetPaymentAccountDto?> GetById(Guid id, CancellationToken ct) => Task.FromResult(id == Account.Id ? Account : null);
    /// <summary>Returns recipient details and counts the read when the id matches, otherwise null.</summary>
    public Task<GetPaymentAccountDetailsDto?> GetSensitiveDetails(Guid id, string actorId, CancellationToken ct)
    {
        if (id != Account.Id)
        {
            return Task.FromResult<GetPaymentAccountDetailsDto?>(null);
        }
        SensitiveReads++;
        return Task.FromResult<GetPaymentAccountDetailsDto?>(new GetPaymentAccountDetailsDto
        {
            Account = Account,
            Recipient = new PaymentRecipientDto
            {
                Kind = PaymentAccountKindDto.Iban,
                SettlementCurrency = CurrencyDto.Euro,
                RecipientName = "Recipient",
                Iban = "DE89370400440532013000",
                Bic = "COBADEFFXXX"
            }
        });
    }
}
