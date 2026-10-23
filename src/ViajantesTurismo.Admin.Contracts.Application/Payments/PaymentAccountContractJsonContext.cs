using System.Text.Json;
using System.Text.Json.Serialization;

namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Owns JSON metadata for payment-account HTTP contracts.</summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(PaymentRecipientDto))]
[JsonSerializable(typeof(CreatePaymentAccountDto))]
[JsonSerializable(typeof(UpdatePaymentAccountDto))]
[JsonSerializable(typeof(VerifyPaymentAccountDto))]
[JsonSerializable(typeof(ReviewPaymentAccountDto))]
[JsonSerializable(typeof(RetirePaymentAccountDto))]
[JsonSerializable(typeof(WithdrawPaymentAccountVerificationDto))]
[JsonSerializable(typeof(GetPaymentAccountDto))]
[JsonSerializable(typeof(GetPaymentAccountDetailsDto))]
[JsonSerializable(typeof(GetPaymentAccountDto[]))]
[JsonSerializable(typeof(IReadOnlyList<GetPaymentAccountDto>))]
public sealed partial class PaymentAccountContractJsonContext : JsonSerializerContext;
