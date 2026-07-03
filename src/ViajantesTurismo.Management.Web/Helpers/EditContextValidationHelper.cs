using Microsoft.AspNetCore.Components.Forms;
using ViajantesTurismo.Common.Contracts;

namespace ViajantesTurismo.Management.Web.Helpers;

/// <summary>
/// Blazor-specific helper for applying server validation errors to EditContext.
/// </summary>
internal static class EditContextValidationHelper
{
    /// <summary>
    /// Applies validation errors from ContractValidationException to the EditContext.
    /// </summary>
    /// <param name="editContext">The EditContext to add field errors to.</param>
    /// <param name="exception">The ContractValidationException containing validation errors.</param>
    public static void ApplyValidationErrors(EditContext editContext, ContractValidationException exception)
        => ApplyValidationErrors(editContext, exception.ValidationErrors);

    private static void ApplyValidationErrors(EditContext editContext, IReadOnlyDictionary<string, string[]> validationErrors)
    {
        if (validationErrors.Count == 0)
        {
            return;
        }

        var messages = new ValidationMessageStore(editContext);

        foreach (var (fieldName, errors) in validationErrors)
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
