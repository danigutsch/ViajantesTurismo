namespace ViajantesTurismo.Management.WebTests.Components.Pages.Payments;

/// <summary>Canned JSON payloads returned by the fake payment HTTP handler.</summary>
internal static class PaymentPageData
{
    /// <summary>JSON list of payment accounts in verified, draft and retired states.</summary>
    public const string Accounts = """
        [
          {
            "id": "00000001-1111-1111-1111-111111111111",
            "revisionId": "00000002-1111-1111-1111-111111111111",
            "version": 7,
            "revision": 2,
            "label": "Verified EUR recipient",
            "kind": 1,
            "status": 1,
            "settlementCurrency": 1,
            "maskedIdentifier": "DE…3000",
            "createdBy": "other-admin",
            "createdAt": "2030-01-05T12:00:00Z",
            "verifiedBy": "other-admin",
            "verifiedAt": "2030-01-05T12:00:00Z",
            "verificationMethod": 1,
            "checkedAt": "2030-01-05T12:00:00Z",
            "selfApproved": true,
            "reviewedBy": "reviewer",
            "reviewedAt": "2030-01-05T12:00:00Z",
            "reviewOutcome": 2,
            "requiresAttention": true
          },
          {
            "id": "00000003-1111-1111-1111-111111111111",
            "revisionId": "00000002-1111-1111-1111-111111111111",
            "version": 7,
            "revision": 2,
            "label": "Draft recipient",
            "kind": 1,
            "status": 0,
            "settlementCurrency": 1,
            "maskedIdentifier": "DE…3000",
            "createdBy": "other-admin",
            "createdAt": "2030-01-05T12:00:00Z",
            "verifiedBy": null,
            "verifiedAt": null,
            "verificationMethod": 1,
            "checkedAt": "2030-01-05T12:00:00Z",
            "selfApproved": false,
            "reviewedBy": "reviewer",
            "reviewedAt": "2030-01-05T12:00:00Z",
            "reviewOutcome": 2,
            "requiresAttention": true
          },
          {
            "id": "00000004-1111-1111-1111-111111111111",
            "revisionId": "00000002-1111-1111-1111-111111111111",
            "version": 7,
            "revision": 2,
            "label": "Retired recipient",
            "kind": 1,
            "status": 2,
            "settlementCurrency": 1,
            "maskedIdentifier": "DE…3000",
            "createdBy": "other-admin",
            "createdAt": "2030-01-05T12:00:00Z",
            "verifiedBy": "other-admin",
            "verifiedAt": "2030-01-05T12:00:00Z",
            "verificationMethod": 1,
            "checkedAt": "2030-01-05T12:00:00Z",
            "selfApproved": true,
            "reviewedBy": "reviewer",
            "reviewedAt": "2030-01-05T12:00:00Z",
            "reviewOutcome": 2,
            "requiresAttention": true
          }
        ]
        """;

    /// <summary>JSON for a single verified payment account.</summary>
    public const string Account = """
        {
          "id": "00000001-1111-1111-1111-111111111111",
          "revisionId": "00000002-1111-1111-1111-111111111111",
          "version": 7,
          "revision": 2,
          "label": "Verified EUR recipient",
          "kind": 1,
          "status": 1,
          "settlementCurrency": 1,
          "maskedIdentifier": "DE…3000",
          "createdBy": "other-admin",
          "createdAt": "2030-01-05T12:00:00Z",
          "verifiedBy": "other-admin",
          "verifiedAt": "2030-01-05T12:00:00Z",
          "verificationMethod": 1,
          "checkedAt": "2030-01-05T12:00:00Z",
          "selfApproved": true,
          "reviewedBy": "reviewer",
          "reviewedAt": "2030-01-05T12:00:00Z",
          "reviewOutcome": 2,
          "requiresAttention": true
        }
        """;

    /// <summary>JSON for a payment recipient with its account.</summary>
    public const string Recipient = """
        {
          "account": {
            "id": "00000001-1111-1111-1111-111111111111",
            "revisionId": "00000002-1111-1111-1111-111111111111",
            "version": 7,
            "revision": 2,
            "label": "Verified EUR recipient",
            "kind": 1,
            "status": 1,
            "settlementCurrency": 1,
            "maskedIdentifier": "DE…3000",
            "createdBy": "other-admin",
            "createdAt": "2030-01-05T12:00:00Z",
            "verifiedBy": "other-admin",
            "verifiedAt": "2030-01-05T12:00:00Z",
            "verificationMethod": 1,
            "checkedAt": "2030-01-05T12:00:00Z",
            "selfApproved": true,
            "reviewedBy": "reviewer",
            "reviewedAt": "2030-01-05T12:00:00Z",
            "reviewOutcome": 2,
            "requiresAttention": true
          },
          "recipient": {
            "kind": 1,
            "settlementCurrency": 1,
            "recipientName": "Checked recipient",
            "iban": "DE89370400440532013000",
            "bic": "COBADEFFXXX"
          }
        }
        """;

    /// <summary>JSON for a booking financial history.</summary>
    public const string History = """
        {
          "bookingId": "00000020-1111-1111-1111-111111111111",
          "version": 12,
          "obligationCurrency": 1,
          "termsStale": false,
          "approvedObligationEuro": 100,
          "outstandingEuro": 50,
          "unallocatedEuroCredit": 70,
          "businessDate": "2030-01-05",
          "overdue": false,
          "schedules": [
            {
              "id": "00000006-1111-1111-1111-111111111111",
              "revision": 1,
              "obligationEuro": 100,
              "bookingPriceEuro": 100,
              "status": 1,
              "approvalWithdrawn": false,
              "approval": {
                "createdBy": "other-admin",
                "createdAt": "2030-01-05T12:00:00Z",
                "approvedBy": "other-admin",
                "approvedAt": "2030-01-05T12:00:00Z",
                "approvalReason": 1,
                "selfApproved": true
              },
              "installments": [
                {
                  "id": "00000005-1111-1111-1111-111111111111",
                  "euroAmount": 100,
                  "dueDate": "2030-01-10",
                  "allocatedEuro": 50,
                  "outstandingEuro": 50
                }
              ]
            },
            {
              "id": "00000007-1111-1111-1111-111111111111",
              "revision": 2,
              "obligationEuro": 100,
              "bookingPriceEuro": 100,
              "status": 0,
              "approvalWithdrawn": false,
              "approval": {
                "createdBy": "other-admin",
                "createdAt": "2030-01-05T12:00:00Z",
                "approvedBy": null,
                "approvedAt": null,
                "approvalReason": 1,
                "selfApproved": false
              },
              "installments": [
                {
                  "id": "00000021-1111-1111-1111-111111111111",
                  "euroAmount": 100,
                  "dueDate": "2030-01-10",
                  "allocatedEuro": 50,
                  "outstandingEuro": 50
                }
              ]
            }
          ],
          "quotes": [
            {
              "id": "00000008-1111-1111-1111-111111111111",
              "brlPerEuro": 5,
              "source": "Agreed bank quote",
              "reference": "masked",
              "agreedAt": "2030-01-05T12:00:00Z",
              "expiresAt": "2030-01-06T12:00:00Z",
              "forRefund": false,
              "approvalWithdrawn": false,
              "approval": {
                "createdBy": "other-admin",
                "createdAt": "2030-01-05T12:00:00Z",
                "approvedBy": "other-admin",
                "approvedAt": "2030-01-05T12:00:00Z",
                "approvalReason": 1,
                "selfApproved": true
              }
            },
            {
              "id": "00000009-1111-1111-1111-111111111111",
              "brlPerEuro": 5,
              "source": "Agreed bank quote",
              "reference": "masked",
              "agreedAt": "2030-01-05T12:00:00Z",
              "expiresAt": "2030-01-06T12:00:00Z",
              "forRefund": false,
              "approvalWithdrawn": false,
              "approval": {
                "createdBy": "other-admin",
                "createdAt": "2030-01-05T12:00:00Z",
                "approvedBy": null,
                "approvedAt": null,
                "approvalReason": 1,
                "selfApproved": false
              }
            },
            {
              "id": "00000010-1111-1111-1111-111111111111",
              "brlPerEuro": 5,
              "source": "Agreed bank quote",
              "reference": "masked",
              "agreedAt": "2030-01-05T12:00:00Z",
              "expiresAt": "2030-01-06T12:00:00Z",
              "forRefund": true,
              "approvalWithdrawn": false,
              "approval": {
                "createdBy": "other-admin",
                "createdAt": "2030-01-05T12:00:00Z",
                "approvedBy": "other-admin",
                "approvedAt": "2030-01-05T12:00:00Z",
                "approvalReason": 1,
                "selfApproved": true
              }
            }
          ],
          "receipts": [
            {
              "id": "00000011-1111-1111-1111-111111111111",
              "settlementCurrency": 1,
              "gross": 120,
              "fees": 0,
              "net": 120,
              "paymentDate": "2030-01-05",
              "settledAt": "2030-01-05T12:00:00Z",
              "reference": "masked",
              "createdBy": "other-admin",
              "createdAt": "2030-01-05T12:00:00Z",
              "method": 3,
              "reversed": false,
              "pendingReconciliation": false,
              "approvedEuroCredit": 120,
              "unallocatedEuroCredit": 70,
              "unallocatedCashCredit": 0,
              "refundableCashBasis": 120,
              "settlementDifference": 0
            },
            {
              "id": "00000012-1111-1111-1111-111111111111",
              "settlementCurrency": 1,
              "gross": 120,
              "fees": 0,
              "net": 120,
              "paymentDate": "2030-01-05",
              "settledAt": "2030-01-05T12:00:00Z",
              "reference": "masked",
              "createdBy": "other-admin",
              "createdAt": "2030-01-05T12:00:00Z",
              "method": 3,
              "reversed": false,
              "pendingReconciliation": true,
              "approvedEuroCredit": 120,
              "unallocatedEuroCredit": 70,
              "unallocatedCashCredit": 0,
              "refundableCashBasis": 120,
              "settlementDifference": 0
            }
          ],
          "reconciliations": [
            {
              "id": "00000013-1111-1111-1111-111111111111",
              "receiptId": "00000011-1111-1111-1111-111111111111",
              "euroCredit": 120,
              "allocations": [],
              "approval": {
                "createdBy": "other-admin",
                "createdAt": "2030-01-05T12:00:00Z",
                "approvedBy": null,
                "approvedAt": null,
                "approvalReason": 1,
                "selfApproved": false
              }
            }
          ],
          "allocations": [
            {
              "id": "00000016-1111-1111-1111-111111111111",
              "receiptId": "00000011-1111-1111-1111-111111111111",
              "installmentId": "00000005-1111-1111-1111-111111111111",
              "originalEuroCredit": 50,
              "activeEuroCredit": 50
            }
          ],
          "refunds": [
            {
              "id": "00000014-1111-1111-1111-111111111111",
              "receiptId": "00000011-1111-1111-1111-111111111111",
              "originalCashBasis": 10,
              "euroCreditReversed": 10,
              "euroCashPayable": 10,
              "actualSettlement": 10,
              "fxDifference": 0,
              "reference": "masked",
              "performedAt": "2030-01-05T12:00:00Z",
              "corrected": false,
              "reversedAllocations": [],
              "approval": {
                "createdBy": "other-admin",
                "createdAt": "2030-01-05T12:00:00Z",
                "approvedBy": null,
                "approvedAt": null,
                "approvalReason": 1,
                "selfApproved": false
              }
            },
            {
              "id": "00000015-1111-1111-1111-111111111111",
              "receiptId": "00000011-1111-1111-1111-111111111111",
              "originalCashBasis": 10,
              "euroCreditReversed": 10,
              "euroCashPayable": 10,
              "actualSettlement": 10,
              "fxDifference": 0,
              "reference": "masked",
              "performedAt": "2030-01-05T12:00:00Z",
              "corrected": false,
              "reversedAllocations": [],
              "approval": {
                "createdBy": "other-admin",
                "createdAt": "2030-01-05T12:00:00Z",
                "approvedBy": "other-admin",
                "approvedAt": "2030-01-05T12:00:00Z",
                "approvalReason": 1,
                "selfApproved": true
              }
            }
          ],
          "reversals": [
            {
              "id": "00000017-1111-1111-1111-111111111111",
              "targetId": "00000018-1111-1111-1111-111111111111",
              "operation": "allocation-release",
              "approvedBy": "other-admin",
              "approvedAt": "2030-01-05T12:00:00Z",
              "reason": 11,
              "allocationIds": [
                "00000016-1111-1111-1111-111111111111"
              ],
              "euroAmount": 10
            }
          ],
          "reviews": [
            {
              "targetId": "00000006-1111-1111-1111-111111111111",
              "targetRevision": 1,
              "reviewerId": "reviewer",
              "reviewedAt": "2030-01-05T12:00:00Z",
              "changesRequested": true
            }
          ]
        }
        """;

    /// <summary>JSON for a payment instruction with its document.</summary>
    public const string Instruction = """
        {
          "document": {
            "id": "00000022-1111-1111-1111-111111111111",
            "bookingId": "00000020-1111-1111-1111-111111111111",
            "revision": 1,
            "templateId": "payment-instructions",
            "templateVersion": "1",
            "sourceVersion": "1",
            "status": 0,
            "fields": [],
            "createdAt": "2030-01-05T12:00:00Z",
            "updatedAt": "2030-01-05T12:00:00Z",
            "hasFinalizedArtifact": false
          },
          "documentVersion": 9,
          "accountId": "00000001-1111-1111-1111-111111111111",
          "accountRevisionId": "00000002-1111-1111-1111-111111111111",
          "maskedRecipient": "DE…3000",
          "eligible": true,
          "euroAmount": 50,
          "settlementAmount": 50,
          "settlementCurrency": 1,
          "paymentReference": "VT-EVIDENCE",
          "approval": {
            "createdBy": "other-admin",
            "createdAt": "2030-01-05T12:00:00Z",
            "approvedBy": "other-admin",
            "approvedAt": "2030-01-05T12:00:00Z",
            "approvalReason": 1,
            "selfApproved": true
          },
          "reviews": [
            {
              "targetId": "00000006-1111-1111-1111-111111111111",
              "targetRevision": 1,
              "reviewerId": "reviewer",
              "reviewedAt": "2030-01-05T12:00:00Z",
              "changesRequested": true
            }
          ]
        }
        """;

}
