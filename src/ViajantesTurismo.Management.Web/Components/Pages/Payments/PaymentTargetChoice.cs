namespace ViajantesTurismo.Management.Web.Components.Pages.Payments;

/// <summary>A display choice for an explicit installment or allocation selection.</summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1515", Justification = "Used by public Blazor component parameters.")]
public sealed record PaymentTargetChoice(Guid Id, string Label);
