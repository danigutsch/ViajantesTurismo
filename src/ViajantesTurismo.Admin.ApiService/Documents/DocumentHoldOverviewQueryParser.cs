using System.Globalization;
using PandaBear.DispositionHold;
using PandaBear.Results;
using ViajantesTurismo.Admin.Application.Documents.Holds;

namespace ViajantesTurismo.Admin.ApiService.Documents;

/// <summary>Parses the raw overview query-string values into a query; a rejected value is named by field and never echoed.</summary>
internal static class DocumentHoldOverviewQueryParser
{
    private const string RoundTripFormat = "O";

    /// <summary>Parses the optional filters and paging, applying the default page and page size when absent.</summary>
    public static Result<ListDocumentHoldOverviewQuery> Parse(string? state, string? basis, string? releasedSince, string? page, string? pageSize)
    {
        if (!TryParseEnum<DocumentHoldState>(state, out var parsedState))
        {
            return Reject("state");
        }

        if (!TryParseEnum<HoldBasis>(basis, out var parsedBasis))
        {
            return Reject("basis");
        }

        if (!TryParseInstant(releasedSince, out var parsedSince))
        {
            return Reject("releasedSince");
        }

        if (!TryParseNumber(page, DocumentHoldOverviewPaging.FirstPage, out var parsedPage))
        {
            return Reject("page");
        }

        if (!TryParseNumber(pageSize, DocumentHoldOverviewPaging.DefaultPageSize, out var parsedPageSize))
        {
            return Reject("pageSize");
        }

        return Result.Ok(new ListDocumentHoldOverviewQuery(parsedState, parsedBasis, parsedSince, parsedPage, parsedPageSize));
    }

    private static Result<ListDocumentHoldOverviewQuery> Reject(string field) =>
        DocumentHoldErrors.InvalidField(field).ConvertError<ListDocumentHoldOverviewQuery>();

    private static bool TryParseEnum<TEnum>(string? value, out TEnum? parsed)
        where TEnum : struct, Enum
    {
        parsed = null;
        if (string.IsNullOrEmpty(value))
        {
            return true;
        }

        if (!Enum.GetNames<TEnum>().Contains(value, StringComparer.Ordinal) || !Enum.TryParse<TEnum>(value, ignoreCase: false, out var member))
        {
            return false;
        }

        parsed = member;
        return true;
    }

    private static bool TryParseInstant(string? value, out DateTimeOffset? parsed)
    {
        parsed = null;
        if (string.IsNullOrEmpty(value))
        {
            return true;
        }

        if (!DateTimeOffset.TryParseExact(value, RoundTripFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var instant))
        {
            return false;
        }

        parsed = instant;
        return true;
    }

    private static bool TryParseNumber(string? value, int fallback, out int parsed)
    {
        parsed = fallback;
        return string.IsNullOrEmpty(value)
            || int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out parsed);
    }
}
