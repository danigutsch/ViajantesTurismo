using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components.Forms;
using SharedKernel.HttpClients;

namespace ViajantesTurismo.Management.Web.Helpers;

/// <summary>
/// Blazor-specific helper for applying server validation errors to EditContext.
/// </summary>
internal static class EditContextValidationHelper
{
    private static readonly ConditionalWeakTable<EditContext, ValidationMessageStore> ServerValidationMessages = [];

    /// <summary>
    /// Applies validation errors from ContractValidationException to the EditContext.
    /// </summary>
    /// <param name="editContext">The EditContext to add field errors to.</param>
    /// <param name="exception">The ContractValidationException containing validation errors.</param>
    public static void ApplyValidationErrors(EditContext editContext, ContractValidationException exception)
        => ApplyValidationErrors(editContext, exception.ValidationErrors);

    /// <summary>
    /// Applies validation errors from a contract command outcome to the EditContext.
    /// </summary>
    /// <param name="editContext">The EditContext to add field errors to.</param>
    /// <param name="validationErrors">The field validation errors.</param>
    public static void ApplyValidationErrors(EditContext editContext, IReadOnlyDictionary<string, string[]> validationErrors)
    {
        if (!ServerValidationMessages.TryGetValue(editContext, out var messages) && validationErrors.Count == 0)
        {
            return;
        }

        messages ??= ServerValidationMessages.GetValue(editContext, static context => new ValidationMessageStore(context));
        messages.Clear();

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
