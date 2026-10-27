namespace ViajantesTurismo.Admin.ApiServiceTests.Payments;

/// <summary>JSON request bodies for payment endpoint boundary tests.</summary>
internal static class PaymentEndpointInputs
{
    /// <summary>Returns a valid account request body for the operation, defaulting to an expected-version confirmation.</summary>
    public static string Account(string operation) => operation switch
    {
        "create" => """{"label":"Checked recipient","recipient":{"kind":1,"settlementCurrency":1,"recipientName":"Checked recipient","iban":"DE89370400440532013000","bic":"COBADEFFXXX"}}""",
        "update" => """{"expectedVersion":2,"label":"Changed recipient","recipient":{"kind":1,"settlementCurrency":1,"recipientName":"Changed recipient","iban":"DE89370400440532013000","bic":"COBADEFFXXX"}}""",
        "verify" => """{"expectedVersion":2,"confirmed":true,"independentCheckAttested":true,"method":1,"checkedAt":"2030-01-05T12:00:00Z"}""",
        "review" => """{"expectedVersion":2,"outcome":1}""",
        "retire" => """{"expectedVersion":2,"reason":7,"confirmed":true}""",
        _ => """{"expectedVersion":2,"confirmed":true}"""
    };

    /// <summary>Returns a valid financial request body for the route template, defaulting to a reason confirmation.</summary>
    public static string ForRoute(string route) => route switch
    {
        "schedules" => """{"expectedVersion":2,"obligationEuro":100,"installments":[{"euroAmount":100,"dueDate":"2030-01-10"}]}""",
        "quotes" => """{"expectedVersion":2,"brlPerEuro":5.25,"source":"Bank","reference":"agreement","agreedAt":"2030-01-05T12:00:00Z","expiresAt":"2030-01-06T12:00:00Z","forRefund":false}""",
        "receipts" => """{"expectedVersion":2,"settlementCurrency":1,"gross":100,"fees":0,"paymentDate":"2030-01-05","method":1,"reference":"receipt","proposedAllocations":[]}""",
        "receipts/{id}/reconciliations" => """{"expectedVersion":2,"euroCredit":100,"allocations":[]}""",
        "receipts/{id}/allocations" or "allocation-releases" => """{"expectedVersion":2,"allocations":[],"reason":11,"confirmed":true}""",
        "receipts/{id}/refunds" => """{"expectedVersion":2,"actualSettlement":10,"originalCashBasis":10,"euroCreditReversed":10,"euroCashPayable":10,"reversedAllocations":[],"reference":"refund","performedAt":"2030-01-05T12:00:00Z"}""",
        "records/{id}/review" => """{"expectedVersion":2,"changesRequested":true}""",
        _ => """{"expectedVersion":2,"reason":1,"confirmed":true}"""
    };
}
