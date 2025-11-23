using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Web.Components.Shared;

namespace ViajantesTurismo.Admin.WebTests.Components.Shared;

public class BookingsListTests : BunitContext
{
    public BookingsListTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Renders_Info_Message_When_Bookings_Is_Null()
    {
        // Arrange
        // Act
        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, null));

        // Assert
        var alert = cut.Find(".alert.alert-info");
        Assert.Contains("No bookings found", alert.TextContent);
    }

    [Fact]
    public void Renders_Info_Message_When_Bookings_Is_Empty()
    {
        // Arrange
        // Act
        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, Array.Empty<GetBookingDto>()));

        // Assert
        var alert = cut.Find(".alert.alert-info");
        Assert.Contains("No bookings found", alert.TextContent);
    }

    [Fact]
    public void Renders_QuickGrid_When_Bookings_Exist()
    {
        // Arrange
        var bookings = new[]
        {
            CreateBooking(Guid.NewGuid(), "Customer 1", "Tour 1")
        };

        // Act
        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, bookings));

        // Assert
        var grid = cut.Find("table.table.table-hover");
        Assert.NotNull(grid);
    }

    [Fact]
    public void Shows_Tour_Info_Column_When_ShowTourInfo_Is_True()
    {
        // Arrange
        var bookings = new[]
        {
            CreateBooking(Guid.NewGuid(), "Customer 1", "Tour 1", tourIdentifier: "TOUR-001")
        };

        // Act
        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, bookings)
            .Add(p => p.ShowTourInfo, true));

        // Assert
        var headers = cut.FindAll("th");
        Assert.Contains(headers, h => h.TextContent.Contains("Tour"));
        Assert.Contains("Tour 1", cut.Markup);
        Assert.Contains("TOUR-001", cut.Markup);
    }

    [Fact]
    public void Hides_Tour_Info_Column_When_ShowTourInfo_Is_False()
    {
        // Arrange
        var bookings = new[]
        {
            CreateBooking(Guid.NewGuid(), "Customer 1", "Tour 1", tourIdentifier: "TOUR-001")
        };

        // Act
        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, bookings)
            .Add(p => p.ShowTourInfo, false));

        // Assert
        var headers = cut.FindAll("th");
        Assert.DoesNotContain(headers, h => h.TextContent.Contains("Tour"));
        Assert.DoesNotContain("Tour 1", cut.Markup);
    }

    [Fact]
    public void Shows_Customer_Info_Column_When_ShowCustomerInfo_Is_True()
    {
        // Arrange
        var bookings = new[]
        {
            CreateBooking(Guid.NewGuid(), "John Doe", "Tour 1")
        };

        // Act
        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, bookings)
            .Add(p => p.ShowCustomerInfo, true));

        // Assert
        var headers = cut.FindAll("th");
        Assert.Contains(headers, h => h.TextContent.Contains("Customer"));
        Assert.Contains("John Doe", cut.Markup);
    }

    [Fact]
    public void Hides_Customer_Info_Column_When_ShowCustomerInfo_Is_False()
    {
        // Arrange
        var bookings = new[]
        {
            CreateBooking(Guid.NewGuid(), "John Doe", "Tour 1")
        };

        // Act
        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, bookings)
            .Add(p => p.ShowCustomerInfo, false));

        // Assert
        var headers = cut.FindAll("th");
        Assert.DoesNotContain(headers, h => h.TextContent.Contains("Customer"));
        Assert.DoesNotContain("John Doe", cut.Markup);
    }

    [Fact]
    public void Displays_Companion_Link_When_Companion_Exists()
    {
        // Arrange
        var companionId = Guid.NewGuid();
        var bookings = new[]
        {
            CreateBooking(Guid.NewGuid(), "Customer 1", "Tour 1", companionId: companionId, companionName: "Jane Doe")
        };

        // Act
        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, bookings));

        // Assert
        var companionLink = cut.Find($"a[href='/customers/{companionId}']");
        Assert.Equal("Jane Doe", companionLink.TextContent);
    }

    [Fact]
    public void Displays_Dash_When_No_Companion()
    {
        // Arrange
        var bookings = new[]
        {
            CreateBooking(Guid.NewGuid(), "Customer 1", "Tour 1", companionId: null, companionName: null)
        };

        // Act
        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, bookings));

        // Assert
        Assert.Contains("<span class=\"text-muted\">-</span>", cut.Markup);
    }

    [Fact]
    public void Displays_Percentage_Discount_Badge()
    {
        // Arrange
        var bookings = new[]
        {
            CreateBooking(Guid.NewGuid(), "Customer 1", "Tour 1",
                discountType: DiscountTypeDto.Percentage,
                discountAmount: 15.5m,
                discountReason: "Early Bird")
        };

        // Act
        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, bookings));

        // Assert
        var badge = cut.Find(".badge.bg-primary");
        Assert.Contains("15.50%", badge.TextContent);
        Assert.Equal("Percentage Discount: Early Bird", badge.GetAttribute("title"));
    }

    [Fact]
    public void Displays_Absolute_Discount_Badge()
    {
        // Arrange
        var bookings = new[]
        {
            CreateBooking(Guid.NewGuid(), "Customer 1", "Tour 1",
                discountType: DiscountTypeDto.Absolute,
                discountAmount: 100.00m,
                discountReason: "Group Discount")
        };

        // Act
        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, bookings));

        // Assert
        var badge = cut.Find(".badge.bg-warning.text-dark");
        Assert.Contains("100.00", badge.TextContent);
        Assert.Equal("Absolute Discount: Group Discount", badge.GetAttribute("title"));
    }

    [Fact]
    public void Displays_Dash_When_No_Discount()
    {
        // Arrange
        var bookings = new[]
        {
            CreateBooking(Guid.NewGuid(), "Customer 1", "Tour 1",
                discountType: DiscountTypeDto.None)
        };

        // Act
        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, bookings));

        // Assert
        var discountCell = cut.FindAll("td")[5]; // Discount column
        Assert.Contains("-", discountCell.TextContent);
    }

    [Fact]
    public void Displays_BookingStatusBadge()
    {
        // Arrange
        var bookings = new[]
        {
            CreateBooking(Guid.NewGuid(), "Customer 1", "Tour 1", status: BookingStatusDto.Confirmed)
        };

        // Act
        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, bookings));

        // Assert
        var statusBadge = cut.FindComponent<BookingStatusBadge>();
        Assert.NotNull(statusBadge);
    }

    [Fact]
    public void Displays_PaymentStatusBadge()
    {
        // Arrange
        var bookings = new[]
        {
            CreateBooking(Guid.NewGuid(), "Customer 1", "Tour 1", paymentStatus: PaymentStatusDto.PartiallyPaid)
        };

        // Act
        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, bookings));

        // Assert
        var paymentBadge = cut.FindComponent<PaymentStatusBadge>();
        Assert.NotNull(paymentBadge);
    }

    [Fact]
    public void Shows_Edit_Button_For_All_Bookings()
    {
        // Arrange
        var bookings = new[]
        {
            CreateBooking(Guid.NewGuid(), "Customer 1", "Tour 1")
        };

        // Act
        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, bookings));

        // Assert
        var editButton = cut.Find("button.btn-primary");
        Assert.Contains("Edit", editButton.TextContent);
        Assert.Contains("bi-pencil", editButton.InnerHtml);
    }

    [Fact]
    public void Shows_Confirm_Button_For_Pending_Bookings()
    {
        // Arrange
        var bookings = new[]
        {
            CreateBooking(Guid.NewGuid(), "Customer 1", "Tour 1", status: BookingStatusDto.Pending)
        };

        // Act
        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, bookings));

        // Assert
        var confirmButton = cut.Find("button.btn-success");
        Assert.Contains("Confirm", confirmButton.TextContent);
        Assert.Contains("bi-check-circle", confirmButton.InnerHtml);
    }

    [Fact]
    public void Does_Not_Show_Confirm_Button_For_Confirmed_Bookings()
    {
        // Arrange
        var bookings = new[]
        {
            CreateBooking(Guid.NewGuid(), "Customer 1", "Tour 1", status: BookingStatusDto.Confirmed)
        };

        // Act
        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, bookings));

        // Assert
        var successButtons = cut.FindAll("button.btn-success");
        Assert.Empty(successButtons);
    }

    [Fact]
    public void Shows_Complete_Button_For_Confirmed_Bookings()
    {
        // Arrange
        var bookings = new[]
        {
            CreateBooking(Guid.NewGuid(), "Customer 1", "Tour 1", status: BookingStatusDto.Confirmed)
        };

        // Act
        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, bookings));

        // Assert
        var completeButton = cut.Find("button.btn-info");
        Assert.Contains("Complete", completeButton.TextContent);
        Assert.Contains("bi-check2-all", completeButton.InnerHtml);
    }

    [Fact]
    public void Does_Not_Show_Complete_Button_For_Pending_Bookings()
    {
        // Arrange
        var bookings = new[]
        {
            CreateBooking(Guid.NewGuid(), "Customer 1", "Tour 1", status: BookingStatusDto.Pending)
        };

        // Act
        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, bookings));

        // Assert
        var infoButtons = cut.FindAll("button.btn-info");
        Assert.Empty(infoButtons);
    }

    [Fact]
    public void Shows_Cancel_Button_For_Pending_Bookings()
    {
        // Arrange
        var bookings = new[]
        {
            CreateBooking(Guid.NewGuid(), "Customer 1", "Tour 1", status: BookingStatusDto.Pending)
        };

        // Act
        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, bookings));

        // Assert
        var cancelButton = cut.Find("button.btn-warning");
        Assert.Contains("Cancel", cancelButton.TextContent);
        Assert.Contains("bi-x-circle", cancelButton.InnerHtml);
    }

    [Fact]
    public void Does_Not_Show_Cancel_Button_For_Completed_Bookings()
    {
        // Arrange
        var bookings = new[]
        {
            CreateBooking(Guid.NewGuid(), "Customer 1", "Tour 1", status: BookingStatusDto.Completed)
        };

        // Act
        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, bookings));

        // Assert
        var warningButtons = cut.FindAll("button.btn-warning");
        Assert.Empty(warningButtons);
    }

    [Fact]
    public void Does_Not_Show_Cancel_Button_For_Cancelled_Bookings()
    {
        // Arrange
        var bookings = new[]
        {
            CreateBooking(Guid.NewGuid(), "Customer 1", "Tour 1", status: BookingStatusDto.Cancelled)
        };

        // Act
        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, bookings));

        // Assert
        var warningButtons = cut.FindAll("button.btn-warning");
        Assert.Empty(warningButtons);
    }

    [Fact]
    public void Shows_Delete_Button_For_All_Bookings()
    {
        // Arrange
        var bookings = new[]
        {
            CreateBooking(Guid.NewGuid(), "Customer 1", "Tour 1")
        };

        // Act
        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, bookings));

        // Assert
        var deleteButton = cut.Find("button.btn-danger");
        Assert.Contains("Delete", deleteButton.TextContent);
        Assert.Contains("bi-trash", deleteButton.InnerHtml);
    }

    [Fact]
    public async Task Invokes_OnEdit_Callback_When_Edit_Button_Clicked()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var editedId = Guid.Empty;
        var bookings = new[]
        {
            CreateBooking(bookingId, "Customer 1", "Tour 1")
        };

        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, bookings)
            .Add(p => p.OnEdit, EventCallback.Factory.Create<Guid>(this, id => editedId = id)));

        // Act
        var editButton = cut.Find("button.btn-primary");
        await editButton.ClickAsync(new());

        // Assert
        Assert.Equal(bookingId, editedId);
    }

    [Fact]
    public async Task Invokes_OnConfirm_Callback_When_Confirm_Button_Clicked()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var confirmedId = Guid.Empty;
        var bookings = new[]
        {
            CreateBooking(bookingId, "Customer 1", "Tour 1", status: BookingStatusDto.Pending)
        };

        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, bookings)
            .Add(p => p.OnConfirm, EventCallback.Factory.Create<Guid>(this, id => confirmedId = id)));

        // Act
        var confirmButton = cut.Find("button.btn-success");
        await confirmButton.ClickAsync(new());

        // Assert
        Assert.Equal(bookingId, confirmedId);
    }

    [Fact]
    public async Task Invokes_OnComplete_Callback_When_Complete_Button_Clicked()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var completedId = Guid.Empty;
        var bookings = new[]
        {
            CreateBooking(bookingId, "Customer 1", "Tour 1", status: BookingStatusDto.Confirmed)
        };

        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, bookings)
            .Add(p => p.OnComplete, EventCallback.Factory.Create<Guid>(this, id => completedId = id)));

        // Act
        var completeButton = cut.Find("button.btn-info");
        await completeButton.ClickAsync(new());

        // Assert
        Assert.Equal(bookingId, completedId);
    }

    [Fact]
    public async Task Invokes_OnCancel_Callback_When_Cancel_Button_Clicked()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var cancelledId = Guid.Empty;
        var bookings = new[]
        {
            CreateBooking(bookingId, "Customer 1", "Tour 1", status: BookingStatusDto.Pending)
        };

        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, bookings)
            .Add(p => p.OnCancel, EventCallback.Factory.Create<Guid>(this, id => cancelledId = id)));

        // Act
        var cancelButton = cut.Find("button.btn-warning");
        await cancelButton.ClickAsync(new());

        // Assert
        Assert.Equal(bookingId, cancelledId);
    }

    [Fact]
    public async Task Invokes_OnDelete_Callback_When_Delete_Button_Clicked()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var deletedId = Guid.Empty;
        var bookings = new[]
        {
            CreateBooking(bookingId, "Customer 1", "Tour 1")
        };

        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, bookings)
            .Add(p => p.OnDelete, EventCallback.Factory.Create<Guid>(this, id => deletedId = id)));

        // Act
        var deleteButton = cut.Find("button.btn-danger");
        await deleteButton.ClickAsync(new());

        // Assert
        Assert.Equal(bookingId, deletedId);
    }

    [Fact]
    public void Does_Not_Show_Paginator_For_10_Or_Fewer_Bookings()
    {
        // Arrange
        var bookings = Enumerable.Range(1, 10)
            .Select(i => CreateBooking(Guid.NewGuid(), $"Customer {i}", "Tour 1"))
            .ToArray();

        // Act
        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, bookings));

        // Assert
        var paginators = cut.FindComponents<Paginator>();
        Assert.Empty(paginators);
    }

    [Fact]
    public void Shows_Paginator_For_More_Than_10_Bookings()
    {
        // Arrange
        var bookings = Enumerable.Range(1, 15)
            .Select(i => CreateBooking(Guid.NewGuid(), $"Customer {i}", "Tour 1"))
            .ToArray();

        // Act
        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, bookings));

        // Assert
        var paginators = cut.FindComponents<Paginator>();
        Assert.NotEmpty(paginators);
    }

    [Fact]
    public void Displays_All_Column_Headers()
    {
        // Arrange
        var bookings = new[]
        {
            CreateBooking(Guid.NewGuid(), "Customer 1", "Tour 1")
        };

        // Act
        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, bookings)
            .Add(p => p.ShowTourInfo, true)
            .Add(p => p.ShowCustomerInfo, true));

        // Assert
        var headers = cut.FindAll("th").Select(h => h.TextContent).ToList();
        Assert.Contains(headers, h => h.Contains("Booking Date"));
        Assert.Contains(headers, h => h.Contains("Tour"));
        Assert.Contains(headers, h => h.Contains("Customer"));
        Assert.Contains(headers, h => h.Contains("Companion"));
        Assert.Contains(headers, h => h.Contains("Total Price"));
        Assert.Contains(headers, h => h.Contains("Discount"));
        Assert.Contains(headers, h => h.Contains("Status"));
        Assert.Contains(headers, h => h.Contains("Payment"));
        Assert.Contains(headers, h => h.Contains("Actions"));
    }

    private static GetBookingDto CreateBooking(
        Guid id,
        string customerName,
        string tourName,
        string? tourIdentifier = null,
        Guid? companionId = null,
        string? companionName = null,
        DiscountTypeDto discountType = DiscountTypeDto.None,
        decimal discountAmount = 0,
        string? discountReason = null,
        BookingStatusDto status = BookingStatusDto.Pending,
        PaymentStatusDto paymentStatus = PaymentStatusDto.Unpaid)
    {
        return new GetBookingDto
        {
            Id = id,
            BookingDate = DateTime.Now,
            TourId = Guid.NewGuid(),
            TourName = tourName,
            TourIdentifier = tourIdentifier ?? "TOUR-001",
            CustomerId = Guid.NewGuid(),
            CustomerName = customerName,
            CompanionId = companionId,
            CompanionName = companionName,
            TotalPrice = 1000m,
            DiscountType = discountType,
            DiscountAmount = discountAmount,
            DiscountReason = discountReason,
            Status = status,
            PaymentStatus = paymentStatus,
            AmountPaid = 0,
            RemainingBalance = 1000m,
            Payments = Array.Empty<GetPaymentDto>(),
            Notes = null
        };
    }
}
