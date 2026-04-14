namespace ViajantesTurismo.Admin.Web.Components.Pages.Customers;

/// <summary>
/// Defines the steps in the customer import page workflow.
/// </summary>
internal enum ImportCustomersWorkflowStep
{
    FileSelection,
    HeaderMapping,
    Preview,
    DuplicateResolution,
}
