using Microsoft.AspNetCore.WebUtilities;
using SharedKernel.Testing.Http;
using System.Globalization;
using System.Net;
using System.Text.Json.Nodes;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Payments;

/// <summary>Provides two independently addressable masked pages and explicit evidence for each fixture record.</summary>
internal sealed class BookingFinancialPagingScenario : IDisposable
{
    private const int ReconciliationSelectionPageSize = 5;
    private const int ReconciliationSelectionCount = 6;
    private static readonly BookingFinancialSection[] Sections =
    [
        BookingFinancialSection.Schedules, BookingFinancialSection.Quotes, BookingFinancialSection.Receipts,
        BookingFinancialSection.Reconciliations, BookingFinancialSection.Allocations, BookingFinancialSection.Refunds,
        BookingFinancialSection.Reversals, BookingFinancialSection.Reviews, BookingFinancialSection.ReceiptChoices,
        BookingFinancialSection.SettlementQuoteChoices, BookingFinancialSection.RefundQuoteChoices,
        BookingFinancialSection.ApprovedInstallmentChoices, BookingFinancialSection.ProposedInstallmentChoices,
        BookingFinancialSection.ActiveAllocationChoices
    ];
    private readonly JsonObject _history = JsonNode.Parse(PaymentPageData.History).ShouldNotBeNull().AsObject();

    /// <summary>Creates the recording transport used by the real financial API client.</summary>
    public BookingFinancialPagingScenario()
    {
        Transport = new RecordingHttpMessageHandler(Respond);
    }

    /// <summary>Gets the transport which captures attempted finance reads.</summary>
    public RecordingHttpMessageHandler Transport
    {
        get;
    }

    /// <summary>Gets or sets the status to return on the next read.</summary>
    public HttpStatusCode? NextReadStatus
    {
        get; set;
    }

    /// <inheritdoc />
    public void Dispose() => Transport.Dispose();

    private HttpResponseMessage Respond(CapturedHttpRequest request)
    {
        if (request.Method == HttpMethod.Post)
        {
            return HttpResponseFactory.Json("""
                {"bookingId":"11111111-1111-1111-1111-111111111111","resourceId":"00000013-1111-1111-1111-111111111111"}
                """, HttpStatusCode.OK);
        }
        if (NextReadStatus is { } error)
        {
            NextReadStatus = null;
            return HttpResponseFactory.Json("{}", error);
        }
        var uri = request.Message.RequestUri.ShouldNotBeNull();
        var query = QueryHelpers.ParseQuery(uri.Query);
        var section = Enum.TryParse<BookingFinancialSection>(query.GetValueOrDefault("section").ToString(), out var requested) ? requested : (BookingFinancialSection?)null;
        var page = int.TryParse(query.GetValueOrDefault("page").ToString(), CultureInfo.InvariantCulture, out var number) ? number : 1;
        var parent = Guid.TryParse(query.GetValueOrDefault("parentId").ToString(), out var parentId) ? parentId : (Guid?)null;
        var evidence = uri.AbsolutePath.EndsWith("/details", StringComparison.Ordinal);
        var history = _history.DeepClone().AsObject();
        var windows = new List<BookingFinancialPageWindowDto>();
        var response = new JsonObject { ["history"] = history };
        foreach (var name in new[] { "schedules", "quotes", "receipts", "reconciliations", "allocations", "refunds", "reversals", "reviews" })
        {
            var category = Enum.Parse<BookingFinancialSection>(name, ignoreCase: true);
            var parentCategory = ChildOf(category);
            var nested = parentCategory is not null && section == parentCategory;
            var item = history[name].ShouldNotBeNull()[0].ShouldNotBeNull().DeepClone().AsObject();
            if (section == category && page == 2)
            {
                AdvanceId(item);
            }
            if (evidence && section == category)
            {
                item["reference"] = "private-page-evidence";
                if (category == BookingFinancialSection.Quotes)
                {
                    item["source"] = "private-quote-source";
                }
            }
            if (parentCategory is { } child)
            {
                if (nested)
                {
                    item["id"] = parent;
                }
                var reconciliation = child == BookingFinancialSection.ReconciliationSelections;
                if (nested || reconciliation)
                {
                    UpdateNestedEntry(item, child, nested ? page : 1);
                }
                if (section is null || section == category || nested)
                {
                    var id = item["id"].ShouldNotBeNull().GetValue<Guid>();
                    var size = reconciliation ? ReconciliationSelectionPageSize : 1;
                    var count = reconciliation ? ReconciliationSelectionCount : 2;
                    windows.Add(new BookingFinancialPageWindowDto(child, id, nested ? page : 1, size, count, 2));
                }
            }
            history[name] = new JsonArray(item);
        }

        foreach (var name in Sections.Where(name => section is null || name == section))
        {
            windows.Add(new BookingFinancialPageWindowDto(name, null, page, 1, 2, 2));
        }
        response["windows"] = new JsonArray([.. windows.Select(window => new JsonObject
        {
            ["section"] = (int)window.Section,
            ["parentId"] = window.ParentId,
            ["page"] = window.Page,
            ["pageSize"] = window.PageSize,
            ["totalCount"] = window.TotalCount,
            ["pageCount"] = window.PageCount
        })]);
        response["receiptChoices"] = Choice("receipts", BookingFinancialSection.ReceiptChoices);
        response["settlementQuoteChoices"] = Choice("quotes", BookingFinancialSection.SettlementQuoteChoices);
        response["refundQuoteChoices"] = Choice("quotes", BookingFinancialSection.RefundQuoteChoices, index: 2);
        response["activeAllocationChoices"] = Choice("allocations", BookingFinancialSection.ActiveAllocationChoices);
        response["approvedInstallmentChoices"] = Installment(BookingFinancialSection.ApprovedInstallmentChoices, schedule: 0);
        response["proposedInstallmentChoices"] = Installment(BookingFinancialSection.ProposedInstallmentChoices, schedule: 1);
        return HttpResponseFactory.Json(response.ToJsonString(), HttpStatusCode.OK);

        JsonArray Choice(string source, BookingFinancialSection choice, int index = 0) =>
            CopyChoice(_history[source].ShouldNotBeNull()[index].ShouldNotBeNull(), choice);

        JsonArray Installment(BookingFinancialSection choice, int schedule) =>
            CopyChoice(_history["schedules"].ShouldNotBeNull()[schedule].ShouldNotBeNull()["installments"].ShouldNotBeNull()[0].ShouldNotBeNull(), choice);

        JsonArray CopyChoice(JsonNode source, BookingFinancialSection choice)
        {
            var item = source.DeepClone().AsObject();
            if (section == choice && page == 2)
            {
                AdvanceId(item);
            }
            return new JsonArray(item);
        }
    }

    private static BookingFinancialSection? ChildOf(BookingFinancialSection section) => section switch
    {
        BookingFinancialSection.Schedules => BookingFinancialSection.ScheduleInstallments,
        BookingFinancialSection.Reconciliations => BookingFinancialSection.ReconciliationSelections,
        BookingFinancialSection.Refunds => BookingFinancialSection.RefundSelections,
        BookingFinancialSection.Reversals => BookingFinancialSection.ReversalLinks,
        _ => null
    };

    private static void AdvanceId(JsonObject item)
    {
        var key = item.ContainsKey("id") ? "id" : "targetId";
        var id = item[key].ShouldNotBeNull().GetValue<Guid>().ToString();
        item[key] = Guid.Parse($"{id[..^1]}2");
    }

    private static void UpdateNestedEntry(JsonObject item, BookingFinancialSection section, int page)
    {
        var id = page == 2 ? Guid.Parse("c5082377-3be8-44d9-9ada-664b30788c02") : Guid.Parse("c5082377-3be8-44d9-9ada-664b30788c01");
        switch (section)
        {
            case BookingFinancialSection.ScheduleInstallments:
                item["installments"].ShouldNotBeNull()[0].ShouldNotBeNull()["id"] = id;
                break;
            case BookingFinancialSection.ReconciliationSelections:
                item["allocations"] = page == 1
                    ? new JsonArray([.. Enumerable.Range(1, ReconciliationSelectionPageSize).Select(index => new JsonObject
                    {
                        ["targetId"] = new Guid(index, 0, 0, new byte[8]),
                        ["euroAmount"] = 2
                    })])
                    : new JsonArray(new JsonObject { ["targetId"] = id, ["euroAmount"] = 2 });
                break;
            case BookingFinancialSection.RefundSelections:
                item["reversedAllocations"] = new JsonArray(new JsonObject { ["targetId"] = id, ["euroAmount"] = 2 });
                break;
            case BookingFinancialSection.ReversalLinks:
                item["allocationIds"] = new JsonArray(JsonValue.Create(id));
                break;
        }
    }
}
