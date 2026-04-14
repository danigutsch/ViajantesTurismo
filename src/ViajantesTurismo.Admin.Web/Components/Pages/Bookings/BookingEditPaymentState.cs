using ViajantesTurismo.Admin.Web.Models;

namespace ViajantesTurismo.Admin.Web.Components.Pages.Bookings;

/// <summary>
/// Manages the payment-form state used by the booking edit page.
/// </summary>
internal sealed class BookingEditPaymentState
{
    internal bool IsFormVisible { get; private set; }

    internal PaymentFormModel FormModel { get; private set; } = new();

    internal bool IsSubmitting { get; private set; }

    internal void ShowForm()
    {
        FormModel = new PaymentFormModel();
        IsFormVisible = true;
    }

    internal void HideForm()
    {
        IsFormVisible = false;
    }

    internal void BeginSubmission()
    {
        IsSubmitting = true;
    }

    internal void EndSubmission()
    {
        IsSubmitting = false;
    }
}
