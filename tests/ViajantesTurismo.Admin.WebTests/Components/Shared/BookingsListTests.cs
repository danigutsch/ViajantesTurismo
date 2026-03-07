using Microsoft.AspNetCore.Components.QuickGrid;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Tests.Shared;
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
        Assert.Contains("No bookings found", alert.TextContent, StringComparison.Ordinal);
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
        Assert.Contains("No bookings found", alert.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_QuickGrid_When_Bookings_Exist()
    {
        // Arrange
        var bookings = new[]
        {
            DtoBuilders.BuildBookingDto(
                id: Guid.NewGuid(),
                tourName: "Tour 1",
                customerName: "Customer 1",
                discountType: DiscountTypeDto.None,
                discountAmount: 0,
                status: BookingStatusDto.Pending,
                paymentStatus: PaymentStatusDto.Unpaid)
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
            DtoBuilders.BuildBookingDto(
                id: Guid.NewGuid(),
                tourName: "Tour 1",
                tourIdentifier: "TOUR-001",
                customerName: "Customer 1",
                discountType: DiscountTypeDto.None,
                discountAmount: 0,
                status: BookingStatusDto.Pending,
                paymentStatus: PaymentStatusDto.Unpaid)
        };

        // Act
        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, bookings)
            .Add(p => p.ShowTourInfo, true));

        // Assert
        var headers = cut.FindAll("th");
        Assert.Contains(headers, h => h.TextContent.Contains("Tour", StringComparison.Ordinal));
        Assert.Contains("Tour 1", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("TOUR-001", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Hides_Tour_Info_Column_When_ShowTourInfo_Is_False()
    {
        // Arrange
        var bookings = new[]
        {
            DtoBuilders.BuildBookingDto(
                id: Guid.NewGuid(),
                tourName: "Tour 1",
                tourIdentifier: "TOUR-001",
                customerName: "Customer 1",
                discountType: DiscountTypeDto.None,
                discountAmount: 0,
                status: BookingStatusDto.Pending,
                paymentStatus: PaymentStatusDto.Unpaid)
        };

        // Act
        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, bookings)
            .Add(p => p.ShowTourInfo, false));

        // Assert
        var headers = cut.FindAll("th");
        Assert.DoesNotContain(headers, h => h.TextContent.Contains("Tour", StringComparison.Ordinal));
        Assert.DoesNotContain("Tour 1", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Shows_Customer_Info_Column_When_ShowCustomerInfo_Is_True()
    {
        // Arrange
        var bookings = new[]
        {
            DtoBuilders.BuildBookingDto(
                id: Guid.NewGuid(),
                tourName: "Tour 1",
                customerName: "John Doe",
                discountType: DiscountTypeDto.None,
                discountAmount: 0,
                status: BookingStatusDto.Pending,
                paymentStatus: PaymentStatusDto.Unpaid)
        };

        // Act
        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, bookings)
            .Add(p => p.ShowCustomerInfo, true));

        // Assert
        var headers = cut.FindAll("th");
        Assert.Contains(headers, h => h.TextContent.Contains("Customer", StringComparison.Ordinal));
        Assert.Contains("John Doe", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Hides_Customer_Info_Column_When_ShowCustomerInfo_Is_False()
    {
        // Arrange
        var bookings = new[]
        {
            DtoBuilders.BuildBookingDto(
                id: Guid.NewGuid(),
                tourName: "Tour 1",
                customerName: "John Doe",
                discountType: DiscountTypeDto.None,
                discountAmount: 0,
                status: BookingStatusDto.Pending,
                paymentStatus: PaymentStatusDto.Unpaid)
        };

        // Act
        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, bookings)
            .Add(p => p.ShowCustomerInfo, false));

        // Assert
        var headers = cut.FindAll("th");
        Assert.DoesNotContain(headers, h => h.TextContent.Contains("Customer", StringComparison.Ordinal));
        Assert.DoesNotContain("John Doe", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Displays_Companion_Link_When_Companion_Exists()
    {
        // Arrange
        var companionId = Guid.NewGuid();
        var bookings = new[]
        {
            DtoBuilders.BuildBookingDto(
                id: Guid.NewGuid(),
                tourName: "Tour 1",
                customerName: "Customer 1",
                companionId: companionId,
                companionName: "Jane Doe",
                discountType: DiscountTypeDto.None,
                discountAmount: 0,
                status: BookingStatusDto.Pending,
                paymentStatus: PaymentStatusDto.Unpaid)
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
            DtoBuilders.BuildBookingDto(
                id: Guid.NewGuid(),
                tourName: "Tour 1",
                customerName: "Customer 1",
                discountType: DiscountTypeDto.None,
                discountAmount: 0,
                status: BookingStatusDto.Pending,
                paymentStatus: PaymentStatusDto.Unpaid)
        };

        // Act
        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, bookings));

        // Assert
        Assert.Contains("<span class=\"text-muted\">-</span>", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Displays_Percentage_Discount_Badge()
    {
        // Arrange
        var bookings = new[]
        {
            DtoBuilders.BuildBookingDto(
                id: Guid.NewGuid(),
                tourName: "Tour 1",
                customerName: "Customer 1",
                discountType: DiscountTypeDto.Percentage,
                discountAmount: 15.5m,
                discountReason: "Early Bird",
                status: BookingStatusDto.Pending,
                paymentStatus: PaymentStatusDto.Unpaid)
        };

        // Act
        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, bookings));

        // Assert
        var badge = cut.Find(".badge.bg-primary");
        Assert.Contains("15.50%", badge.TextContent, StringComparison.Ordinal);
        Assert.Equal("Percentage Discount: Early Bird", badge.GetAttribute("title"));
    }

    [Fact]
    public void Displays_Absolute_Discount_Badge()
    {
        // Arrange
        var bookings = new[]
        {
            DtoBuilders.BuildBookingDto(
                id: Guid.NewGuid(),
                tourName: "Tour 1",
                customerName: "Customer 1",
                discountType: DiscountTypeDto.Absolute,
                discountAmount: 100.00m,
                discountReason: "Group Discount",
                status: BookingStatusDto.Pending,
                paymentStatus: PaymentStatusDto.Unpaid,
                currency: CurrencyDto.Real)
        };

        // Act
        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, bookings));

        // Assert
        var badge = cut.Find(".badge.bg-warning.text-dark");
        Assert.Contains("100", badge.TextContent, StringComparison.Ordinal); // Currency formatting will include the amount
        Assert.Equal("Absolute Discount: Group Discount", badge.GetAttribute("title"));
    }

    [Theory]
    [InlineData(CurrencyDto.Real, "R$")]
    [InlineData(CurrencyDto.Euro, "€")]
    [InlineData(CurrencyDto.UsDollar, "$")]
    public void Displays_Discount_With_Correct_Currency_Symbol(CurrencyDto currency, string expectedSymbol)
    {
        // Arrange
        var bookings = new[]
        {
            DtoBuilders.BuildBookingDto(
                id: Guid.NewGuid(),
                tourName: "Tour 1",
                customerName: "Customer 1",
                discountType: DiscountTypeDto.Absolute,
                discountAmount: 150.00m,
                status: BookingStatusDto.Pending,
                paymentStatus: PaymentStatusDto.Unpaid,
                currency: currency)
        };

        // Act
        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, bookings));

        // Assert
        var badge = cut.Find(".badge.bg-warning.text-dark");
        Assert.Contains(expectedSymbol, badge.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Displays_Dash_When_No_Discount()
    {
        // Arrange
        var bookings = new[]
        {
            DtoBuilders.BuildBookingDto(
                id: Guid.NewGuid(),
                tourName: "Tour 1",
                customerName: "Customer 1",
                discountType: DiscountTypeDto.None,
                discountAmount: 0,
                status: BookingStatusDto.Pending,
                paymentStatus: PaymentStatusDto.Unpaid)
        };

        // Act
        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, bookings));

        // Assert
        var discountCell = cut.FindAll("td")[5]; // Discount column
        Assert.Contains("-", discountCell.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Displays_BookingStatusBadge()
    {
        // Arrange
        var bookings = new[]
        {
            DtoBuilders.BuildBookingDto(
                id: Guid.NewGuid(),
                tourName: "Tour 1",
                customerName: "Customer 1",
                discountType: DiscountTypeDto.None,
                discountAmount: 0,
                status: BookingStatusDto.Confirmed,
                paymentStatus: PaymentStatusDto.Unpaid)
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
            DtoBuilders.BuildBookingDto(
                id: Guid.NewGuid(),
                tourName: "Tour 1",
                customerName: "Customer 1",
                discountType: DiscountTypeDto.None,
                discountAmount: 0,
                status: BookingStatusDto.Pending,
                paymentStatus: PaymentStatusDto.PartiallyPaid)
        };

        // Act
        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, bookings));

        // Assert
        var paymentBadge = cut.FindComponent<PaymentStatusBadge>();
        Assert.NotNull(paymentBadge);
    }

    [Fact]
    public void Shows_View_And_Edit_Links_For_All_Bookings()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var bookings = new[]
        {
            DtoBuilders.BuildBookingDto(
                id: bookingId,
                tourName: "Tour 1",
                customerName: "Customer 1",
                discountType: DiscountTypeDto.None,
                discountAmount: 0,
                status: BookingStatusDto.Pending,
                paymentStatus: PaymentStatusDto.Unpaid)
        };

        // Act
        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, bookings));

        // Assert
        var viewLink = cut.Find($"a[href='/bookings/{bookingId}']");
        Assert.Contains("View", viewLink.TextContent, StringComparison.Ordinal);
        Assert.Contains("bi-eye", viewLink.InnerHtml, StringComparison.Ordinal);

        var editLink = cut.Find($"a[href='/bookings/{bookingId}/edit']");
        Assert.Contains("Edit", editLink.TextContent, StringComparison.Ordinal);
        Assert.Contains("bi-pencil", editLink.InnerHtml, StringComparison.Ordinal);
    }

    [Fact]
    public void Does_Not_Show_Paginator_For_10_Or_Fewer_Bookings()
    {
        // Arrange
        var bookings = Enumerable.Range(1, 10)
            .Select(i =>
            {
                string customerName = $"Customer {i}";
                return DtoBuilders.BuildBookingDto(
                    id: Guid.NewGuid(),
                    tourName: "Tour 1",
                    customerName: customerName,
                    discountType: DiscountTypeDto.None,
                    discountAmount: 0,
                    status: BookingStatusDto.Pending,
                    paymentStatus: PaymentStatusDto.Unpaid);
            })
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
            .Select(i =>
            {
                string customerName = $"Customer {i}";
                return DtoBuilders.BuildBookingDto(
                    id: Guid.NewGuid(),
                    tourName: "Tour 1",
                    customerName: customerName,
                    discountType: DiscountTypeDto.None,
                    discountAmount: 0,
                    status: BookingStatusDto.Pending,
                    paymentStatus: PaymentStatusDto.Unpaid);
            })
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
            DtoBuilders.BuildBookingDto(
                id: Guid.NewGuid(),
                tourName: "Tour 1",
                customerName: "Customer 1",
                discountType: DiscountTypeDto.None,
                discountAmount: 0,
                status: BookingStatusDto.Pending,
                paymentStatus: PaymentStatusDto.Unpaid)
        };

        // Act
        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, bookings)
            .Add(p => p.ShowTourInfo, true)
            .Add(p => p.ShowCustomerInfo, true));

        // Assert
        var headers = cut.FindAll("th").Select(h => h.TextContent).ToList();
        Assert.Contains(headers, h => h.Contains("Booking Date", StringComparison.Ordinal));
        Assert.Contains(headers, h => h.Contains("Tour", StringComparison.Ordinal));
        Assert.Contains(headers, h => h.Contains("Customer", StringComparison.Ordinal));
        Assert.Contains(headers, h => h.Contains("Companion", StringComparison.Ordinal));
        Assert.Contains(headers, h => h.Contains("Total Price", StringComparison.Ordinal));
        Assert.Contains(headers, h => h.Contains("Discount", StringComparison.Ordinal));
        Assert.Contains(headers, h => h.Contains("Status", StringComparison.Ordinal));
        Assert.Contains(headers, h => h.Contains("Payment", StringComparison.Ordinal));
        Assert.Contains(headers, h => h.Contains("Actions", StringComparison.Ordinal));
    }
}
