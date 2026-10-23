using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Mappings;
using ViajantesTurismo.Admin.Application.Payments;
using ViajantesTurismo.Admin.Contracts.Application;
using PandaBear.BuildingBlocks;

namespace ViajantesTurismo.Admin.Infrastructure.Payments;

internal sealed class PaymentAccountQueryService(IPaymentAccountStore store, IUnitOfWork unitOfWork, TimeProvider clock) : IPaymentAccountQueryService
{
    public async Task<IReadOnlyList<GetPaymentAccountDto>> List(CancellationToken ct) => (await store.List(ct)).Select(PaymentAccountMappings.ToSummary).ToArray();

    public async Task<GetPaymentAccountDto?> GetById(Guid id, CancellationToken ct)
    {
        var account = await store.GetById(id, ct);
        return account is null ? null : PaymentAccountMappings.ToSummary(account);
    }

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
