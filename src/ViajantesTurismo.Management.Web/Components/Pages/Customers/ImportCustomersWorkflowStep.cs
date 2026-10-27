namespace ViajantesTurismo.Management.Web.Components.Pages.Customers;

/// <summary>
/// Defines the steps in the customer import page workflow.
/// </summary>
internal enum ImportCustomersWorkflowStep
{
    /// <summary>Customer file is being chosen.</summary>
    FileSelection,
    /// <summary>Source columns are being mapped to customer fields.</summary>
    HeaderMapping,
    /// <summary>Parsed rows are being previewed before import.</summary>
    Preview,
    /// <summary>Rows matching existing customers are being resolved.</summary>
    DuplicateResolution,
}
