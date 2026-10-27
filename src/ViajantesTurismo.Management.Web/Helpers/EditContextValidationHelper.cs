using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components.Forms;
using PandaBear.HttpClients;

namespace ViajantesTurismo.Management.Web.Helpers;

/// <summary>
/// Blazor-specific helper for applying server validation errors to EditContext.
/// </summary>
internal static class EditContextValidationHelper
{
    private const int MaxMessageLength = 300;

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

    /// <summary>
    /// Applies server validation errors whose keys match a known model field to the EditContext, bounding each message length.
    /// Messages for unknown keys are discarded.
    /// </summary>
    /// <param name="editContext">The EditContext to add field errors to.</param>
    /// <param name="validationErrors">The server validation errors, if any.</param>
    /// <param name="knownFields">The model property names that errors may be attached to.</param>
    /// <returns>True when at least one error exists and every error key matched a known field; otherwise false.</returns>
    public static bool ApplyKnownFieldErrors(
        EditContext editContext,
        IReadOnlyDictionary<string, string[]>? validationErrors,
        IReadOnlyCollection<string> knownFields)
    {
        if (validationErrors is not { Count: > 0 })
        {
            return false;
        }

        var mapped = new Dictionary<string, string[]>(StringComparer.Ordinal);
        var allKnown = true;
        foreach (var (key, errors) in validationErrors)
        {
            var field = knownFields.FirstOrDefault(known => string.Equals(known, key, StringComparison.OrdinalIgnoreCase));
            if (field is null)
            {
                allKnown = false;
                continue;
            }

            var bounded = errors
                .Where(static error => !string.IsNullOrWhiteSpace(error))
                .Select(static error => error.Length <= MaxMessageLength ? error : string.Concat(error.AsSpan(0, MaxMessageLength - 1), "\u2026"))
                .ToArray();
            if (bounded.Length == 0)
            {
                allKnown = false;
                continue;
            }

            mapped[field] = mapped.TryGetValue(field, out var existing) ? [.. existing, .. bounded] : bounded;
        }

        ApplyValidationErrors(editContext, mapped);
        return allKnown && mapped.Count > 0;
    }
}
