using JetBrains.Annotations;
using SharedKernel.Results;

namespace ViajantesTurismo.Admin.Domain.Documents;

/// <summary>
/// Represents a classified, stable field in a document draft.
/// </summary>
public sealed class DocumentField
{
    private DocumentField(
        string fieldId,
        string label,
        string value,
        DocumentPrivacyClassification privacyClassification,
        bool isEditable)
    {
        FieldId = fieldId;
        Label = label;
        Value = value;
        PrivacyClassification = privacyClassification;
        IsEditable = isEditable;
    }

    /// <summary>
    /// DO NOT USE. This constructor is required by Entity Framework Core for materialisation.
    /// </summary>
    [UsedImplicitly]
    private DocumentField()
    {
    }

    /// <summary>Gets the stable template field identifier.</summary>
    public string FieldId { get; private set; } = default!;

    /// <summary>Gets the customer-visible field label.</summary>
    public string Label { get; private set; } = default!;

    /// <summary>Gets the generated field value.</summary>
    public string Value { get; private set; } = default!;

    /// <summary>Gets the field privacy classification.</summary>
    public DocumentPrivacyClassification PrivacyClassification { get; private set; }

    /// <summary>Gets whether staff may override this field.</summary>
    public bool IsEditable { get; private set; }

    /// <summary>Gets the optional staff override without replacing generated source text.</summary>
    public string? StaffOverride { get; private set; }

    /// <summary>Gets the value to render for this field.</summary>
    public string RenderedValue => StaffOverride ?? Value;

    /// <summary>Creates a classified document field.</summary>
    public static Result<DocumentField> Create(
        string fieldId,
        string label,
        string value,
        DocumentPrivacyClassification privacyClassification,
        bool isEditable)
    {
        ArgumentNullException.ThrowIfNull(fieldId);
        ArgumentNullException.ThrowIfNull(label);
        ArgumentNullException.ThrowIfNull(value);

        if (string.IsNullOrWhiteSpace(fieldId))
        {
            return DocumentErrors.ValueRequired("fieldId").ConvertError<DocumentField>();
        }

        if (fieldId.Length > DocumentLimits.MaxFieldIdLength)
        {
            return DocumentErrors.ValueTooLong("fieldId", DocumentLimits.MaxFieldIdLength).ConvertError<DocumentField>();
        }

        if (string.IsNullOrWhiteSpace(label))
        {
            return DocumentErrors.ValueRequired("label").ConvertError<DocumentField>();
        }

        if (label.Length > DocumentLimits.MaxFieldLabelLength)
        {
            return DocumentErrors.ValueTooLong("label", DocumentLimits.MaxFieldLabelLength).ConvertError<DocumentField>();
        }

        if (value.Length > DocumentLimits.MaxFieldValueLength)
        {
            return DocumentErrors.ValueTooLong("value", DocumentLimits.MaxFieldValueLength).ConvertError<DocumentField>();
        }

        if (privacyClassification == DocumentPrivacyClassification.Unclassified)
        {
            return DocumentErrors.UnclassifiedField(fieldId).ConvertError<DocumentField>();
        }

        return Result.Ok(new DocumentField(fieldId, label, value, privacyClassification, isEditable));
    }

    /// <summary>Sets a staff override for an editable field.</summary>
    public Result SetStaffOverride(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (!IsEditable)
        {
            return DocumentErrors.FieldIsNotEditable(FieldId);
        }

        if (value.Length > DocumentLimits.MaxFieldValueLength)
        {
            return DocumentErrors.ValueTooLong("value", DocumentLimits.MaxFieldValueLength);
        }

        StaffOverride = value;
        return Result.Ok();
    }

    internal DocumentField CopyWithCompatibleOverride(DocumentField previous)
    {
        if (previous.IsEditable && IsEditable && previous.StaffOverride is not null)
        {
            StaffOverride = previous.StaffOverride;
        }

        return this;
    }
}
