using Microsoft.AspNetCore.Components.Forms;
using ViajantesTurismo.Admin.Web.Exceptions;

namespace ViajantesTurismo.Admin.Web.Helpers;

/// <summary>
/// Blazor-specific helper for applying server validation errors to EditContext.
/// </summary>
internal static class EditContextValidationHelper
{
    /// <summary>
    /// Applies validation errors from ApiValidationException to the EditContext.
    /// </summary>
    /// <param name="editContext">The EditContext to add field errors to.</param>
    /// <param name="exception">The ApiValidationException containing validation errors.</param>
    public static void ApplyValidationErrors(EditContext editContext, ApiValidationException exception)
    {
        if (exception.ValidationErrors.Count == 0)
        {
            return;
        }

        var messages = new ValidationMessageStore(editContext);

        foreach (var (fieldName, errors) in exception.ValidationErrors)
        {
            var field = editContext.Field(fieldName);
            foreach (var error in errors)
            {
                messages.Add(field, error);
            }
        }

        editContext.NotifyValidationStateChanged();
    }
}
