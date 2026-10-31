namespace ViajantesTurismo.Management.Web.Components.Pages.Documents;

/// <summary>Holds the values typed into the place-hold form: opaque codes and dates only.</summary>
internal sealed class PlaceHoldFormModel
{
    /// <summary>Gets or sets the selected hold basis.</summary>
    public string Basis { get; set; } = string.Empty;

    /// <summary>Gets or sets the opaque purpose code.</summary>
    public string PurposeCode { get; set; } = string.Empty;

    /// <summary>Gets or sets the opaque authority reference.</summary>
    public string AuthorityReference { get; set; } = string.Empty;

    /// <summary>Gets or sets the date the hold is next due for review.</summary>
    public DateOnly? ReviewDueDate
    {
        get; set;
    }

    /// <summary>Gets or sets the optional date the hold expires by itself.</summary>
    public DateOnly? ExpiryDate
    {
        get; set;
    }
}
