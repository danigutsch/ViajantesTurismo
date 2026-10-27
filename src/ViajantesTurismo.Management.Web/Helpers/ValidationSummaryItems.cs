using Microsoft.AspNetCore.Components.Forms;
using PandaBear.Blazor;

namespace ViajantesTurismo.Management.Web.Helpers;

/// <summary>Builds error summary entries from the validation messages shown on an edit context.</summary>
internal static class ValidationSummaryItems
{
    /// <summary>
    /// Lists every validation message of the given fields, client-side and server-bound, linking each to its control.
    /// A new list is returned on every call, so the summary takes focus again for each submission.
    /// </summary>
    /// <param name="editContext">The edit context whose messages are listed.</param>
    /// <param name="fields">Maps each model property name to the element id of its control, in display order.</param>
    /// <returns>One entry per message.</returns>
    public static IReadOnlyList<ErrorSummaryItem> From(EditContext editContext, IReadOnlyList<KeyValuePair<string, string>> fields)
    {
        ArgumentNullException.ThrowIfNull(editContext);
        ArgumentNullException.ThrowIfNull(fields);

        return
        [
            .. fields.SelectMany(field => editContext
                .GetValidationMessages(editContext.Field(field.Key))
                .Select(message => new ErrorSummaryItem(field.Value, message)))
        ];
    }
}
