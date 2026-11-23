using Microsoft.Extensions.DependencyInjection.Extensions;
using ViajantesTurismo.Admin.Api;
using ViajantesTurismo.Admin.Application.Payments;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.ApiServiceTests.Payments;

/// <summary>Query service exposing one verified EUR account and counting sensitive-detail reads.</summary>
internal sealed class PaymentAccountQueryProbe : IPaymentAccountQueryService
{
    /// <summary>Creates the payment read test host with this probe and the requested feature state.</summary>
    public WebApplicationFactory<AdminApiHostEntryPoint> CreateHost(bool enabled) =>
        AdminApiTestHost.Create(services =>
        {
            services.RemoveAll<IPaymentAccountQueryService>();
            services.AddSingleton<IPaymentAccountQueryService>(this);
        }, configureConfiguration: configuration => configuration["Payments:Enabled"] = enabled.ToString());

    /// <summary>Gets how many times sensitive account details were read.</summary>
    public int SensitiveReads
    {
        get; private set;
    }

    /// <summary>Gets the paging and filter arguments most recently sent to the probe.</summary>
    public (int? Page, int PageSize, bool VerifiedOnly)? LastPageRequest
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
        SettlementCurrency = "EUR",
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
    /// <inheritdoc />
    public Task<GetPaymentAccountPageDto> ListPage(int? page, int pageSize, bool verifiedOnly, CancellationToken ct)
    {
        LastPageRequest = (page, pageSize, verifiedOnly);
        return Task.FromResult(new GetPaymentAccountPageDto
        {
            Items = [Account],
            Page = 1,
            PageSize = pageSize,
            Total = 1
        });
    }
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
                SettlementCurrency = "EUR",
                RecipientName = "Recipient",
                Iban = "DE89370400440532013000",
                Bic = "COBADEFFXXX"
            }
        });
    }
}
