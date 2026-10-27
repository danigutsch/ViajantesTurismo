using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Mappings;
using ViajantesTurismo.Admin.Application.Payments;
using ViajantesTurismo.Admin.Contracts.Application;
using PandaBear.BuildingBlocks;

namespace ViajantesTurismo.Admin.Infrastructure.Payments;

/// <summary>Reads payment accounts and audits sensitive detail access.</summary>
/// <param name="store">The payment account store.</param>
/// <param name="unitOfWork">Saves the access audit row.</param>
/// <param name="clock">The clock for audit timestamps.</param>
internal sealed class PaymentAccountQueryService(IPaymentAccountStore store, IUnitOfWork unitOfWork, TimeProvider clock) : IPaymentAccountQueryService
{
    /// <summary>Lists all payment account summaries.</summary>
    public async Task<IReadOnlyList<GetPaymentAccountDto>> List(CancellationToken ct) => [.. (await store.List(ct)).Select(PaymentAccountMappings.ToSummary)];

    /// <summary>Returns the account summary, or null when not found.</summary>
    public async Task<GetPaymentAccountDto?> GetById(Guid id, CancellationToken ct)
    {
        var account = await store.GetById(id, ct);
        return account is null ? null : PaymentAccountMappings.ToSummary(account);
    }

    /// <summary>Returns the decrypted recipient details after recording an audit row for the actor, or null when not found.</summary>
    public async Task<GetPaymentAccountDetailsDto?> GetSensitiveDetails(Guid id, string actorId, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        var account = await store.GetById(id, ct);
        if (account is null)
        {
            return null;
        }
        var state = account.Current.Recipient.ExportState();
        var recipient = new PaymentRecipientDto
        {
            SettlementCurrency = TourMapper.MapToCurrencyDto(state.SettlementCurrency),
            Kind = PaymentAccountMappings.Map(state.Kind),
            RecipientName = state.RecipientName,
            Iban = state.Iban,
            Bic = state.Bic,
            PixKey = state.PixKey,
            PixKeyType = state.PixType switch
            {
                PixKeyType.None => PixKeyTypeDto.None,
                PixKeyType.Cpf => PixKeyTypeDto.Cpf,
                PixKeyType.Cnpj => PixKeyTypeDto.Cnpj,
                PixKeyType.Phone => PixKeyTypeDto.Phone,
                PixKeyType.Email => PixKeyTypeDto.Email,
                PixKeyType.Evp => PixKeyTypeDto.Evp,
                _ => throw new InvalidOperationException("Invalid protected Pix key type.")
            },
            Ispb = state.Ispb,
            Branch = state.Branch,
            AccountNumber = state.AccountNumber,
            AccountType = PaymentAccountMappings.Map(state.AccountType),
            TaxId = state.TaxId
        };
        store.AuditAccess(account, actorId, clock.GetUtcNow());
        await unitOfWork.SaveEntities(ct);
        return new GetPaymentAccountDetailsDto { Account = PaymentAccountMappings.ToSummary(account), Recipient = recipient };
    }
}
